-- Migration 002: indexes adicionais e seed inicial
-- Executar após 001_initial_schema.sql

-- Índice para busca de empresa por CNPJ
CREATE INDEX IF NOT EXISTS idx_companies_taxid ON companies(taxid);

-- Índice para busca de usuário por username (já tem UNIQUE, mas explicita)
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- Índice para busca de produto por empresa
CREATE INDEX IF NOT EXISTS idx_products_companyid ON products(companyid);

-- Índice para busca de clientes por empresa
CREATE INDEX IF NOT EXISTS idx_clients_companyid ON clients(companyid);

-- Índice para busca de estoque por empresa
CREATE INDEX IF NOT EXISTS idx_stocks_companyid ON stocks(companyid);

-- ============================================================
-- SEED: empresa e usuário admin padrão
-- Senha: Admin@123 (BCrypt hash gerado em runtime — substituir)
-- ============================================================
-- ATENÇÃO: substitua o hash abaixo pelo gerado pela aplicação.
-- Para gerar: BCrypt.Net.BCrypt.HashPassword("Admin@123")
-- O valor abaixo é apenas um placeholder de exemplo.
-- ============================================================

INSERT INTO companies (name, taxid, address, createdat, updatedat)
VALUES ('Empresa Demo', '00.000.000/0001-00', 'Endereço Demo, 123', now(), now())
ON CONFLICT DO NOTHING;

-- Inserir admin somente se não existir
-- Hash deve ser substituído por um valor real gerado pela aplicação
-- INSERT INTO users (username, passwordhash, fullname, role, companyid, createdat, updatedat)
-- SELECT 'admin', '<HASH_BCRYPT_AQUI>', 'Administrador', 'Admin', id, now(), now()
-- FROM companies WHERE taxid = '00.000.000/0001-00'
-- ON CONFLICT (username) DO NOTHING;
