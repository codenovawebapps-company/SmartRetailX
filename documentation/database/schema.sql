-- ============================================================
-- SmartRetailX — RDS PostgreSQL Schema
-- Database: smartretailx
-- ============================================================

-- Enable UUID extension (optional, for future migration from int PKs)
-- CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- TABLE: users
-- Owned by: UserService
-- ============================================================
CREATE TABLE IF NOT EXISTS users (
    id            SERIAL          PRIMARY KEY,
    name          VARCHAR(150)    NOT NULL,
    email         VARCHAR(255)    NOT NULL UNIQUE,
    password_hash VARCHAR(255)    NOT NULL,
    role          VARCHAR(50)     NOT NULL DEFAULT 'Customer',  -- Customer | Admin | Vendor
    created_at    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_email ON users (email);

-- ============================================================
-- TABLE: orders
-- Owned by: OrderService
-- ============================================================
CREATE TABLE IF NOT EXISTS orders (
    id           SERIAL          PRIMARY KEY,
    user_id      INT             NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status       VARCHAR(50)     NOT NULL DEFAULT 'Pending',  -- Pending | Processing | Shipped | Delivered | Cancelled
    total_amount NUMERIC(12, 2)  NOT NULL DEFAULT 0.00,
    order_date   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_orders_user_id ON orders (user_id);
CREATE INDEX idx_orders_status  ON orders (status);

-- ============================================================
-- TABLE: order_items
-- Owned by: OrderService (child of orders)
-- ============================================================
CREATE TABLE IF NOT EXISTS order_items (
    id           SERIAL          PRIMARY KEY,
    order_id     INT             NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id   VARCHAR(100)    NOT NULL,   -- References DynamoDB Products (no FK)
    product_name VARCHAR(255)    NOT NULL,
    quantity     INT             NOT NULL CHECK (quantity > 0),
    unit_price   NUMERIC(12, 2)  NOT NULL CHECK (unit_price >= 0)
);

CREATE INDEX idx_order_items_order_id   ON order_items (order_id);
CREATE INDEX idx_order_items_product_id ON order_items (product_id);

-- ============================================================
-- TABLE: payments
-- Owned by: PaymentService
-- ============================================================
CREATE TABLE IF NOT EXISTS payments (
    id               SERIAL          PRIMARY KEY,
    order_id         INT             NOT NULL REFERENCES orders(id),
    user_id          INT             NOT NULL REFERENCES users(id),
    amount           NUMERIC(12, 2)  NOT NULL,
    currency         VARCHAR(10)     NOT NULL DEFAULT 'USD',
    status           VARCHAR(50)     NOT NULL DEFAULT 'Pending',  -- Pending | Success | Failed | Refunded
    payment_method   VARCHAR(50)     NOT NULL DEFAULT 'Card',     -- Card | UPI | Wallet | COD
    transaction_ref  VARCHAR(255),
    paid_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_payments_order_id ON payments (order_id);
CREATE INDEX idx_payments_status   ON payments (status);

-- ============================================================
-- Trigger: auto-update updated_at columns
-- ============================================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trg_orders_updated_at
    BEFORE UPDATE ON orders
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
