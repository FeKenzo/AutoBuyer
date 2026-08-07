# Configuração da ingestão do Telegram

O projeto `AutoBuyer.TelegramIngestion.Worker` usa uma conta dedicada do
Telegram para ler o canal público `@NesTechPromocoes`. Ele importa as
publicações recentes na inicialização e passa a tratar novas publicações e
edições em tempo real.

## 1. Preparar a conta dedicada

1. Crie ou escolha uma conta separada para a ingestão.
2. Inscreva essa conta em <https://t.me/NesTechPromocoes>.
3. Entre em <https://my.telegram.org> com essa conta.
4. Em **API development tools**, crie uma aplicação e anote `api_id` e
   `api_hash`.

Não envie `api_hash`, código de verificação, senha de 2FA nem o arquivo de
sessão para o repositório.

## 2. Configurar os segredos locais

Na pasta do worker, use User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=autobuyer;Username=postgres;Password=SUA_SENHA"
dotnet user-secrets set "TelegramIngestion:ApiId" "SEU_API_ID"
dotnet user-secrets set "TelegramIngestion:ApiHash" "SEU_API_HASH"
dotnet user-secrets set "TelegramIngestion:PhoneNumber" "+55SEUNUMERO"
dotnet user-secrets set "TelegramIngestion:Enabled" "true"
```

O primeiro login pedirá o código recebido no Telegram pelo console. Se a
conta tiver 2FA, ele também solicitará a senha. Para execução não interativa,
as variáveis `TELEGRAM_VERIFICATION_CODE` e `TELEGRAM_2FA_PASSWORD` podem ser
usadas apenas durante o primeiro login e removidas em seguida.

## 3. Atualizar o banco e executar

```bash
dotnet ef database update --project ../AutoBuyer.Infrastructure --startup-project .
dotnet run
```

Depois do primeiro login, a autorização fica no arquivo configurado em
`TelegramIngestion:SessionPath`. Esse arquivo concede acesso à conta e deve
ser tratado como segredo.

## Comportamento de importação

- `TelegramChatId + TelegramMessageId` identifica a publicação.
- Publicações editadas atualizam o mesmo `PromotionCandidate`.
- `StoreId + ExternalProductId` identifica o `ProductTarget`.
- Repetições do mesmo produto atualizam `LastObservedPrice` e `LastSeenAt`.
- `TargetPrice` não recebe automaticamente o preço promocional.
- Terabyte e Pichau ficam elegíveis ao monitoramento, mas só entram no ciclo
  depois que um `TargetPrice` for definido. As demais lojas começam com
  monitoramento desabilitado.
- Links encurtados são resolvidos antes da identificação da loja e produto.
- Lojas desconhecidas são cadastradas pela origem do link, mas seus alvos
  permanecem sem monitoramento automático.
