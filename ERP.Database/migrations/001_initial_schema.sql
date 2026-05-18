-- Initial schema for Nina ERP

CREATE TABLE
IF NOT EXISTS companies
(
    id serial PRIMARY KEY,
    name varchar
(200) NOT NULL,
    taxid varchar
(50),
    address varchar
(500),
    createdat timestamptz NOT NULL DEFAULT now
(),
    updatedat timestamptz NOT NULL DEFAULT now
()
);

CREATE TABLE
IF NOT EXISTS users
(
    id serial PRIMARY KEY,
    username varchar
(100) NOT NULL UNIQUE,
    passwordhash text NOT NULL,
    fullname varchar
(200),
    role varchar
(50),
    companyid int NOT NULL REFERENCES companies
(id) ON
DELETE CASCADE,
    createdat timestamptz
NOT NULL DEFAULT now
(),
    updatedat timestamptz NOT NULL DEFAULT now
()
);

CREATE TABLE
IF NOT EXISTS clients
(
    id serial PRIMARY KEY,
    name varchar
(200) NOT NULL,
    document varchar
(50),
    email varchar
(200),
    phone varchar
(50),
    address varchar
(500),
    companyid int NOT NULL REFERENCES companies
(id) ON
DELETE CASCADE,
    createdat timestamptz
NOT NULL DEFAULT now
(),
    updatedat timestamptz NOT NULL DEFAULT now
()
);

CREATE TABLE
IF NOT EXISTS products
(
    id serial PRIMARY KEY,
    name varchar
(200) NOT NULL,
    sku varchar
(100),
    description text,
    price numeric
(18,2) DEFAULT 0,
    companyid int NOT NULL REFERENCES companies
(id) ON
DELETE CASCADE,
    createdat timestamptz
NOT NULL DEFAULT now
(),
    updatedat timestamptz NOT NULL DEFAULT now
()
);

CREATE TABLE
IF NOT EXISTS stocks
(
    id serial PRIMARY KEY,
    productid int NOT NULL REFERENCES products
(id) ON
DELETE CASCADE,
    quantity int
NOT NULL DEFAULT 0,
    location varchar
(100),
    companyid int NOT NULL REFERENCES companies
(id) ON
DELETE CASCADE,
    createdat timestamptz
NOT NULL DEFAULT now
(),
    updatedat timestamptz NOT NULL DEFAULT now
()
);

-- Indexes
CREATE INDEX
IF NOT EXISTS idx_products_sku ON products
(sku);
CREATE INDEX
IF NOT EXISTS idx_clients_document ON clients
(document);
CREATE INDEX
IF NOT EXISTS idx_stocks_productid ON stocks
(productid);
