# src/AgentService/config.py
# Same Key Vault pattern as src/config.ts on the Node side (KEY_VAULT_ENABLED
# + KEY_VAULT_NAME, DefaultAzureCredential, kebab-case secret names) - kept
# as a near-literal port so the two services behave identically for anyone
# who already read one of them. Called once, early in main.py's import
# chain, right after load_dotenv() so KEY_VAULT_ENABLED/KEY_VAULT_NAME are
# already in os.environ by the time this runs.
#
# DefaultAzureCredential resolves to the VM's Managed Identity in
# production and to the local `az login` session in dev - same as the Node
# side, same reason: one code path, no separate "how do I auth locally"
# story to maintain.

import os

from azure.core.exceptions import ResourceNotFoundError
from azure.identity import DefaultAzureCredential
from azure.keyvault.secrets import SecretClient

# Only the secrets this service actually reads from the environment -
# GOOGLE_SERVICE_ACCOUNT_FILE is a file PATH, not a secret value, so it's
# not in this list (the file itself needs to exist on disk either way;
# Key Vault stores values, not files).
SECRET_ENV_VARS = {
    "ANTHROPIC_API_KEY": "anthropic-api-key",
    "PAGSEGURO_TOKEN": "pagseguro-token",
    "STRIPE_SECRET_KEY": "stripe-secret-key",
    "ORDER_API_AUTH_VALUE": "order-api-auth-value",
    "CATALOG_API_AUTH_VALUE": "catalog-api-auth-value",
}


def load_secrets_from_key_vault() -> None:
    if os.getenv("KEY_VAULT_ENABLED", "false").lower() != "true":
        return

    vault_name = os.environ.get("KEY_VAULT_NAME")
    if not vault_name:
        raise RuntimeError("KEY_VAULT_ENABLED=true but KEY_VAULT_NAME is not set in the environment.")

    client = SecretClient(
        vault_url=f"https://{vault_name}.vault.azure.net",
        credential=DefaultAzureCredential(),
    )

    for env_var, secret_name in SECRET_ENV_VARS.items():
        try:
            secret = client.get_secret(secret_name)
            if secret.value:
                os.environ[env_var] = secret.value
        except ResourceNotFoundError:
            # Not configured for this client - leave whatever (if anything)
            # was already in the local .env, same "missing = skip" rule as
            # the Node side's loadSecretsFromKeyVault().
            continue
