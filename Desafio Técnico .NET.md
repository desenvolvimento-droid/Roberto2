# Desafio Técnico – Desenvolvedor(a) .NET  
## Plataforma de Processamento de Transações Financeiras

## 📌 Sobre a PagueVeloz

A **PagueVeloz** é uma empresa de tecnologia focada no setor financeiro, especializada em soluções de meios de pagamento, serviços bancários integrados e adquirência.

Com arquitetura orientada a **microsserviços**, alto volume de transações e foco em **escalabilidade, performance e segurança**, buscamos profissionais que valorizem **arquitetura limpa, código de qualidade e excelência técnica**.

---

## 🧠 Contexto do Desafio

Você foi alocado em um time responsável por construir o **núcleo transacional** de uma nova plataforma de adquirência, parte de um ecossistema distribuído.

Esse núcleo deve operar em um ambiente crítico, lidando com:
- Alto volume de transações
- Concorrência
- Confiabilidade
- Consistência de dados

---

## 🎯 Objetivo

Construir um sistema para **processamento de operações financeiras**, com suporte a:

- Múltiplas contas por cliente
- Limite de crédito operacional
- Transações reversíveis
- Processamento assíncrono
- Resiliência a falhas
- Controle de concorrência

> O projeto deve estar preparado para futura separação em **microsserviços**.

---

## 📐 Regras de Negócio

### 👤 Clientes e Contas
- Cada cliente pode possuir **N contas**
- Cada conta possui:
  - Saldo disponível
  - Saldo reservado
  - Limite de crédito
  - Status

### 💳 Operações Financeiras
Tipos de operações suportadas:
- Crédito
- Débito
- Reserva
- Captura
- Estorno
- Transferência

Regras:
- Operações devem considerar **saldo disponível + limite**
- Validações devem impedir estados inconsistentes

---

### 🔒 Concorrência e Lock
- Operações concorrentes **na mesma conta** devem ser bloqueadas
- Garantir consistência durante o lock
- Evitar condições de corrida (race conditions)

---

### 🔁 Resiliência e Eventos
- Cada operação deve gerar **eventos assíncronos**
- Simular falhas na publicação de eventos
- Implementar **retry com backoff exponencial**

---

### ⏳ Consistência Eventual
- Atualização de saldo pode ocorrer de forma **eventual**, via eventos

---

### 🧾 Histórico e Auditoria
- Registrar todas as operações com:
  - Tipo
  - Status
  - Timestamps
- Histórico imutável para auditoria

---

### ♻️ Rollback e Idempotência
- Garantir **idempotência** das operações
- Aplicar rollback em caso de falha
- Evitar duplicidade de processamento

---

## ⚙️ Requisitos Técnicos Avaliados

- Programação assíncrona (`async/await`)
- Uso eficiente de memória
- SOLID, OOP, polimorfismo
- Padrões de projeto
- Código testável
- Arquitetura escalável:
  - Clean Architecture
  - DDD
  - Onion Architecture
- Resiliência:
  - Retry
  - Fallback
- Transações distribuídas
- Modelagem relacional adequada

---

## 🔍 O Que Será Avaliado Além do Código

- Modelagem de domínio
- Clareza das decisões técnicas
- Organização e legibilidade
- Estratégia de testes
- Controle de concorrência
- Criatividade na solução
- Cobertura de testes consistente

---

## 📦 Entregáveis

- Repositório público (GitHub)
- README com instruções claras de execução
- Código em **C# (.NET 9)**
- Cobertura de testes automatizados

> ⚠️ O projeto será executado — **cada detalhe importa**.

---

## ⭐ Diferenciais (Opcional)

- Uso de Docker
- Métricas de performance
- Observabilidade
- Eventos de negócio
- Deploy em nuvem ou container

---

## 🚀 Considerações Finais

Este desafio busca avaliar não apenas sua capacidade de escrever código, mas também sua visão arquitetural, maturidade técnica e capacidade de lidar com sistemas críticos de alta concorrência.

Boa sorte! 🚀
