import { useEffect, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertCircle,
  Banknote,
  Barcode,
  CreditCard,
  Minus,
  PauseCircle,
  Plus,
  RotateCcw,
  ShoppingCart,
  Trash2,
} from 'lucide-react'
import { cashService } from '../../services/cash'
import { inventoryService } from '../../services/inventory'
import { productsService } from '../../services/products'
import { salesService, type PaymentInput, type PaymentMethod } from '../../services/sales'
import type { Product, Sale, SaleItem } from '../../types'

type CartLine = { product: Product; qty: number; discount: number }
type PayLine = { id: string; method: PaymentMethod; amount: number }

const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const paymentLabels: Record<PaymentMethod, string> = {
  Cash: 'Dinheiro',
  CreditCard: 'Crédito',
  DebitCard: 'Débito',
  Pix: 'PIX',
  BankSlip: 'Boleto',
  Check: 'Cheque',
  Other: 'Outro',
}

function recoveredProduct(item: SaleItem): Product {
  return {
    id: item.productId,
    sku: 'VENDA-RECUPERADA',
    name: item.productName,
    description: '',
    ncm: '',
    unit: 'UN',
    priceSale: item.unitPrice,
    priceMinimum: 0,
    costAverage: 0,
    stockQty: Number.MAX_SAFE_INTEGER,
    stockMin: 0,
    brand: '',
    department: '',
    barcode: '',
    isActive: true,
  }
}

export default function PosPage() {
  const queryClient = useQueryClient()
  const searchRef = useRef<HTMLInputElement>(null)
  const recentRef = useRef<HTMLElement>(null)

  const [search, setSearch] = useState('')
  const [cart, setCart] = useState<CartLine[]>([])
  const [message, setMessage] = useState('')
  const [registerId, setRegisterId] = useState('')
  const [opening, setOpening] = useState(0)
  const [warehouseId, setWarehouseId] = useState('')
  const [saleKey, setSaleKey] = useState(() => crypto.randomUUID())
  const [currentSaleId, setCurrentSaleId] = useState<string | null>(null)
  const [recoveredLocked, setRecoveredLocked] = useState(false)
  const [payments, setPayments] = useState<PayLine[]>([
    { id: crypto.randomUUID(), method: 'Cash', amount: 0 },
  ])

  const products = useQuery({
    queryKey: ['products', 'pos', search],
    queryFn: () => productsService.getAll(search || undefined),
  })
  const recent = useQuery({
    queryKey: ['sales', 'recent'],
    queryFn: salesService.getRecent,
    refetchInterval: 10_000,
  })
  const registers = useQuery({ queryKey: ['cash', 'registers'], queryFn: cashService.registers })
  const session = useQuery({
    queryKey: ['cash', 'open'],
    queryFn: cashService.openSession,
    refetchInterval: 10_000,
  })
  const warehouses = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: inventoryService.warehouses })

  const totals = useMemo(() => {
    const gross = cart.reduce((sum, line) => sum + line.product.priceSale * line.qty, 0)
    const discount = cart.reduce((sum, line) => sum + line.discount, 0)
    return { gross, discount, net: gross - discount }
  }, [cart])
  const paid = payments.reduce((sum, payment) => sum + (Number(payment.amount) || 0), 0)

  useEffect(() => {
    if (warehouseId) return
    const defaultWarehouse = warehouses.data?.data?.find((item) => item.isDefault) ?? warehouses.data?.data?.[0]
    if (defaultWarehouse) setWarehouseId(defaultWarehouse.id)
  }, [warehouseId, warehouses.data])

  useEffect(() => {
    setPayments((current) =>
      current.length === 1 ? [{ ...current[0], amount: Math.max(0, totals.net) }] : current,
    )
  }, [totals.net])

  const resetSale = () => {
    setCart([])
    setCurrentSaleId(null)
    setRecoveredLocked(false)
    setSaleKey(crypto.randomUUID())
    setPayments([{ id: crypto.randomUUID(), method: 'Cash', amount: 0 }])
  }

  const createBackendSale = async () => {
    const cashSessionId = session.data?.data?.id
    if (!cashSessionId) throw new Error('Abra o caixa antes da venda.')
    if (!warehouseId) throw new Error('Selecione um depósito.')
    if (currentSaleId) return currentSaleId

    const response = await salesService.create({
      cashSessionId,
      warehouseId,
      idempotencyKey: saleKey,
      items: cart.map((line) => ({
        productId: line.product.id,
        qty: line.qty,
        discount: line.discount,
      })),
    })
    if (!response.data) throw new Error(response.message || 'Venda não criada.')
    setCurrentSaleId(response.data)
    return response.data
  }

  const openCash = useMutation({
    mutationFn: () => cashService.open(registerId, opening),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cash'] })
      setMessage('Caixa aberto com sucesso.')
    },
    onError: (error: any) => setMessage(error.response?.data?.message || 'Não foi possível abrir o caixa.'),
  })

  const finish = useMutation({
    mutationFn: async () => {
      if (!cart.length) throw new Error('Adicione produtos.')
      if (Math.abs(paid - totals.net) > 0.009) throw new Error('A soma dos pagamentos deve ser igual ao total.')

      const saleId = await createBackendSale()
      const cashSessionId = session.data?.data?.id
      if (!cashSessionId) throw new Error('A sessão de caixa foi encerrada. Atualize a tela.')

      const paymentInputs: PaymentInput[] = payments.map((payment) => ({
        method: payment.method,
        amount: Number(payment.amount),
        idempotencyKey: payment.id,
        provider: payment.method === 'Cash' ? 'manual' : 'NotConfigured',
      }))
      return salesService.finalize(saleId, cashSessionId, paymentInputs)
    },
    onSuccess: (result) => {
      const status = result.data?.status
      if (status === 'PaymentPending') {
        setMessage('Pagamento pendente. O estoque permanecerá intacto até a confirmação externa.')
      } else if (status === 'ReconciliationRequired') {
        setMessage('Venda em conciliação. Nenhuma confirmação externa foi presumida.')
      } else {
        setMessage('Venda concluída e estoque movimentado.')
      }
      resetSale()
      queryClient.invalidateQueries()
    },
    onError: (error: any) => setMessage(error.response?.data?.message || error.message || 'Falha ao finalizar.'),
  })

  const suspend = useMutation({
    mutationFn: async () => {
      if (!cart.length) throw new Error('Carrinho vazio.')
      const saleId = await createBackendSale()
      await salesService.suspend(saleId)
      return saleId
    },
    onSuccess: () => {
      setMessage('Venda suspensa. Ela pode ser recuperada pelas vendas recentes.')
      resetSale()
      queryClient.invalidateQueries({ queryKey: ['sales'] })
    },
    onError: (error: any) => setMessage(error.response?.data?.message || error.message),
  })

  const recover = async () => {
    const sale = (recent.data?.data ?? []).find((item: Sale) => item.status === 'Suspended')
    if (!sale) {
      setMessage('Nenhuma venda suspensa encontrada.')
      return
    }

    await salesService.resume(sale.id)
    setCart(
      sale.items.map((item) => ({
        product: recoveredProduct(item),
        qty: item.qty,
        discount: item.discount,
      })),
    )
    setCurrentSaleId(sale.id)
    setRecoveredLocked(true)
    setPayments([{ id: crypto.randomUUID(), method: 'Cash', amount: sale.totalProducts - sale.totalDiscount }])
    setMessage('Venda recuperada. Os itens permanecem bloqueados para preservar o pedido original.')
    queryClient.invalidateQueries({ queryKey: ['sales'] })
  }

  const addProduct = (product: Product) => {
    if (recoveredLocked) return
    setCart((current) => {
      const found = current.find((line) => line.product.id === product.id)
      if (found) {
        return current.map((line) =>
          line.product.id === product.id ? { ...line, qty: line.qty + 1 } : line,
        )
      }
      return [...current, { product, qty: 1, discount: 0 }]
    })
  }

  const changeQty = (productId: string, delta: number) => {
    if (recoveredLocked) return
    setCart((current) =>
      current.map((line) =>
        line.product.id === productId ? { ...line, qty: Math.max(1, line.qty + delta) } : line,
      ),
    )
  }

  useEffect(() => {
    const handleShortcut = (event: KeyboardEvent) => {
      if (event.key === 'F2') {
        event.preventDefault()
        searchRef.current?.focus()
      } else if (event.key === 'F7') {
        event.preventDefault()
        setPayments([{ id: crypto.randomUUID(), method: 'Cash', amount: totals.net }])
      } else if (event.key === 'F8') {
        event.preventDefault()
        setPayments([{ id: crypto.randomUUID(), method: 'Pix', amount: totals.net }])
      } else if (event.key === 'F9') {
        event.preventDefault()
        setPayments([{ id: crypto.randomUUID(), method: 'CreditCard', amount: totals.net }])
      } else if (event.key === 'F10') {
        event.preventDefault()
        if (!suspend.isPending && cart.length) suspend.mutate()
      } else if (event.key === 'F11') {
        event.preventDefault()
        void recover()
      } else if (event.key === 'F12') {
        event.preventDefault()
        recentRef.current?.scrollIntoView({ behavior: 'smooth' })
      } else if (event.key === 'Escape' && !currentSaleId) {
        event.preventDefault()
        resetSale()
      } else if (event.key === 'Enter' && event.ctrlKey) {
        event.preventDefault()
        if (!finish.isPending && cart.length) finish.mutate()
      }
    }

    window.addEventListener('keydown', handleShortcut)
    return () => window.removeEventListener('keydown', handleShortcut)
  }, [cart.length, currentSaleId, finish.isPending, suspend.isPending, totals.net])

  if (!session.isLoading && !session.data?.data) {
    return (
      <div className="p-6 max-w-3xl mx-auto">
        <div className="rounded-3xl border border-amber-200 bg-amber-50 p-7">
          <div className="flex gap-3">
            <AlertCircle className="text-amber-600" />
            <div>
              <h2 className="text-xl font-semibold">Abra o caixa para usar o PDV</h2>
              <p className="mt-1 text-sm text-amber-800">Uma sessão ativa é obrigatória antes de qualquer venda.</p>
            </div>
          </div>
          <div className="mt-6 grid md:grid-cols-2 gap-4">
            <select
              value={registerId}
              onChange={(event) => setRegisterId(event.target.value)}
              className="rounded-xl border px-3 py-3 bg-white"
            >
              <option value="">Selecione o caixa</option>
              {(registers.data?.data ?? []).map((register) => (
                <option key={register.id} value={register.id}>
                  {register.code} · {register.name}
                </option>
              ))}
            </select>
            <input
              type="number"
              min="0"
              step="0.01"
              value={opening}
              onChange={(event) => setOpening(Number(event.target.value))}
              className="rounded-xl border px-3 py-3"
              placeholder="Saldo inicial"
            />
          </div>
          <button
            disabled={!registerId || openCash.isPending}
            onClick={() => openCash.mutate()}
            className="mt-4 rounded-xl bg-indigo-600 text-white px-5 py-3 font-medium disabled:opacity-50"
          >
            Abrir caixa
          </button>
          {message && <p className="mt-4 text-sm">{message}</p>}
        </div>
      </div>
    )
  }

  return (
    <div className="p-6 max-w-[1600px] mx-auto space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-xs font-semibold tracking-[.18em] text-indigo-600 uppercase">Operação comercial</p>
          <h2 className="text-3xl font-semibold mt-1">PDV</h2>
          <p className="text-sm text-slate-500">Caixa aberto · pagamentos idempotentes · baixa somente após captura.</p>
        </div>
        <div className="flex gap-2">
          <span className="rounded-xl bg-emerald-50 px-4 py-2 text-sm text-emerald-700">
            <Banknote size={15} className="inline mr-2" />
            Caixa {session.data?.data?.registerName}
          </span>
          <select
            value={warehouseId}
            onChange={(event) => setWarehouseId(event.target.value)}
            className="rounded-xl border border-slate-200 bg-white px-3 text-sm"
          >
            {(warehouses.data?.data ?? []).map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>{warehouse.name}</option>
            ))}
          </select>
        </div>
      </header>

      {message && (
        <div className="rounded-xl border border-indigo-100 bg-indigo-50 px-4 py-3 text-sm text-indigo-900">
          {message}
        </div>
      )}

      <div className="grid xl:grid-cols-[1.25fr_.75fr] gap-5">
        <section className="rounded-2xl border border-slate-200 bg-white overflow-hidden">
          <div className="p-4 border-b">
            <div className="relative">
              <Barcode className="absolute left-3 top-3 text-indigo-600" size={20} />
              <input
                ref={searchRef}
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Código de barras, SKU ou produto — F2"
                className="w-full rounded-xl border border-slate-200 pl-11 pr-3 py-3 outline-none focus:border-indigo-500"
              />
            </div>
          </div>
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-px bg-slate-100 max-h-[560px] overflow-auto">
            {(products.data?.data ?? []).map((product) => (
              <button
                key={product.id}
                disabled={recoveredLocked || product.stockQty <= 0}
                onClick={() => addProduct(product)}
                className="bg-white p-4 text-left hover:bg-indigo-50 disabled:opacity-50"
              >
                <p className="font-medium">{product.name}</p>
                <p className="text-xs text-slate-500 mt-1">{product.sku} · saldo {product.stockQty}</p>
                <p className="mt-3 font-semibold">{money.format(product.priceSale)}</p>
              </button>
            ))}
          </div>
        </section>

        <aside className="rounded-2xl border border-slate-200 bg-white p-4 h-fit">
          <div className="flex items-center gap-2">
            <ShoppingCart size={18} className="text-indigo-600" />
            <h3 className="font-semibold">Venda atual</h3>
            <span className="ml-auto text-xs text-slate-500">{cart.length} linhas</span>
          </div>

          {recoveredLocked && (
            <div className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-800">
              Venda recuperada: itens bloqueados para preservar a operação persistida.
            </div>
          )}

          <div className="mt-4 space-y-2 max-h-72 overflow-auto">
            {cart.map((line) => (
              <div key={line.product.id} className="rounded-xl border p-3">
                <div className="flex justify-between">
                  <span className="text-sm font-medium">{line.product.name}</span>
                  <button
                    disabled={recoveredLocked}
                    onClick={() => setCart((current) => current.filter((item) => item.product.id !== line.product.id))}
                    aria-label={`Remover ${line.product.name}`}
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
                <div className="mt-2 flex justify-between items-center">
                  <div className="flex border rounded-lg">
                    <button disabled={recoveredLocked} onClick={() => changeQty(line.product.id, -1)} className="p-1.5"><Minus size={13} /></button>
                    <span className="px-3 py-1 text-sm">{line.qty}</span>
                    <button disabled={recoveredLocked} onClick={() => changeQty(line.product.id, 1)} className="p-1.5"><Plus size={13} /></button>
                  </div>
                  <strong>{money.format(line.product.priceSale * line.qty - line.discount)}</strong>
                </div>
              </div>
            ))}
            {!cart.length && <p className="py-10 text-center text-sm text-slate-400">Carrinho vazio</p>}
          </div>

          <div className="mt-4 border-t pt-4">
            <div className="flex justify-between text-sm text-slate-500"><span>Subtotal</span><span>{money.format(totals.gross)}</span></div>
            <div className="flex justify-between text-sm text-slate-500 mt-1"><span>Descontos</span><span>{money.format(totals.discount)}</span></div>
            <div className="flex justify-between text-xl font-semibold mt-2"><span>Total</span><span>{money.format(totals.net)}</span></div>
          </div>

          <div className="mt-4 space-y-2">
            <div className="flex justify-between">
              <label className="text-xs font-semibold text-slate-500 uppercase">Pagamentos</label>
              <button
                onClick={() => setPayments((current) => [...current, { id: crypto.randomUUID(), method: 'Pix', amount: Math.max(0, totals.net - paid) }])}
                className="text-xs text-indigo-600"
              >
                + pagamento misto
              </button>
            </div>
            {payments.map((payment) => (
              <div key={payment.id} className="grid grid-cols-[1fr_120px_30px] gap-2">
                <select
                  value={payment.method}
                  onChange={(event) => setPayments((current) => current.map((item) => item.id === payment.id ? { ...item, method: event.target.value as PaymentMethod } : item))}
                  className="rounded-lg border px-2 py-2 text-sm"
                >
                  {Object.entries(paymentLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
                <input
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={payment.amount}
                  onChange={(event) => setPayments((current) => current.map((item) => item.id === payment.id ? { ...item, amount: Number(event.target.value) } : item))}
                  className="rounded-lg border px-2 text-sm"
                />
                <button disabled={payments.length === 1} onClick={() => setPayments((current) => current.filter((item) => item.id !== payment.id))}>×</button>
              </div>
            ))}
            <p className={`text-xs ${Math.abs(paid - totals.net) <= 0.009 ? 'text-emerald-600' : 'text-amber-700'}`}>
              Informado {money.format(paid)} de {money.format(totals.net)}.
            </p>
          </div>

          <button
            disabled={!cart.length || finish.isPending || Math.abs(paid - totals.net) > 0.009}
            onClick={() => finish.mutate()}
            className="mt-4 w-full rounded-xl bg-emerald-600 text-white py-3 font-semibold disabled:opacity-40 flex justify-center gap-2"
          >
            <CreditCard size={18} /> Finalizar · Ctrl+Enter
          </button>
          <div className="grid grid-cols-2 gap-2 mt-2">
            <button
              disabled={!cart.length || suspend.isPending}
              onClick={() => suspend.mutate()}
              className="rounded-xl border py-2.5 text-sm flex items-center justify-center gap-2"
            >
              <PauseCircle size={16} /> Suspender F10
            </button>
            <button onClick={() => void recover()} className="rounded-xl border py-2.5 text-sm flex items-center justify-center gap-2">
              <RotateCcw size={16} /> Recuperar F11
            </button>
          </div>
        </aside>
      </div>

      <section ref={recentRef} className="rounded-2xl border bg-white overflow-hidden">
        <div className="px-4 py-3 border-b font-medium">Vendas recentes · F12</div>
        <div className="overflow-auto">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-slate-500">
              <tr>
                <th className="p-3 text-left">Data</th>
                <th className="p-3 text-left">Venda</th>
                <th className="p-3 text-left">Status</th>
                <th className="p-3 text-left">Pagamento</th>
                <th className="p-3 text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {(recent.data?.data ?? []).slice(0, 12).map((sale) => (
                <tr key={sale.id} className="border-t">
                  <td className="p-3">{new Date(sale.saleDate).toLocaleString('pt-BR')}</td>
                  <td className="p-3 font-mono text-xs">{sale.id.slice(0, 8)}</td>
                  <td className="p-3">{sale.status}</td>
                  <td className="p-3">{sale.payments?.map((payment) => `${payment.method}: ${payment.status}`).join(' · ') || '—'}</td>
                  <td className="p-3 text-right font-medium">{money.format(sale.totalProducts - sale.totalDiscount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}
