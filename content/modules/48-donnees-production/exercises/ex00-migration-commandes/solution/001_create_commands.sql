BEGIN;

CREATE TABLE commands (
    id UUID PRIMARY KEY,
    idempotency_key VARCHAR(100) NOT NULL,
    status VARCHAR(32) NOT NULL,
    payload JSONB NOT NULL,
    version BIGINT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX ux_commands_idempotency_key ON commands (idempotency_key);
CREATE INDEX ix_commands_status_created_at ON commands (status, created_at, id);

COMMIT;
