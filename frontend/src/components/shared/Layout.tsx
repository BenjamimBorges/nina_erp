import { Outlet, NavLink, useNavigate } from "react-router-dom";
import { useAuthStore } from "../../store/auth";
import { Home, Package, Users, Truck, BarChart3, LogOut } from "lucide-react";

const nav = [
  { to: "/dashboard", icon: Home, label: "Dashboard" },
  { to: "/products", icon: Package, label: "Produtos" },
  { to: "/clients", icon: Users, label: "Clientes" },
  { to: "/suppliers", icon: Truck, label: "Fornecedores" },
  { to: "/stock", icon: BarChart3, label: "Estoque" },
];

export default function Layout() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="flex h-screen">
      <aside className="w-56 bg-indigo-700 flex flex-col">
        <div className="px-4 py-5 border-b border-indigo-600">
          <h1 className="text-white font-semibold text-lg">Nina ERP</h1>
          <p className="text-indigo-300 text-xs mt-0.5">{user?.fullName}</p>
        </div>
        <nav className="flex-1 p-3 space-y-1">
          {nav.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                  isActive
                    ? "bg-indigo-600 text-white"
                    : "text-indigo-200 hover:bg-indigo-600 hover:text-white"
                }`
              }
            >
              <Icon size={16} />
              {label}
            </NavLink>
          ))}
        </nav>
        <button
          onClick={handleLogout}
          className="flex items-center gap-3 px-6 py-4 text-indigo-300 hover:text-white text-sm transition-colors border-t border-indigo-600"
        >
          <LogOut size={16} />
          Sair
        </button>
      </aside>
      <main className="flex-1 overflow-auto bg-gray-50">
        <Outlet />
      </main>
    </div>
  );
}
