-- Ambiente LOCAL de desenvolvimento apenas (produção: Render Postgres).
-- A role de aplicação é criada SEM BYPASSRLS de propósito: é isso que faz
-- o Row-Level Security valer para a API (superusuários ignoram RLS).
-- A API se conecta como techpro_app; as policies com FORCE valem até para
-- o dono das tabelas.
CREATE ROLE techpro_app LOGIN PASSWORD 'techpro_dev'
    NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;

CREATE DATABASE techpro OWNER techpro_app;
