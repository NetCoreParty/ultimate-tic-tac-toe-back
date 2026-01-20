# Secrets management with .dotnet-sops

This repo uses **.dotnet-sops** + SOPS to store encrypted JSON secrets in git and decrypt them at runtime.

## Files

- `.sops.yaml` - encryption rules (update with your key provider)
- `secrets/appsettings.Production.secrets.json` - encrypted prod secrets (commit to git)
- `Sops:SecretsFile` - optional config key to override the secrets file path (default: `secrets/appsettings.Production.secrets.json`)

## Setup (one-time)

1. Install the tool:
   ```bash
   dotnet tool restore
   ```
2. Install `age` (includes `age-keygen`) on Windows (choose one):
   ```powershell
   winget install --id FiloSottile.age
   ```
   ```powershell
   choco install age
   ```
   ```powershell
   scoop install age
   ```
3. Generate an age key pair and copy the public key into `.sops.yaml`:
   ```powershell
   age-keygen -o key.txt
   ```
   The file contains:
   - Public key (recipient): `age1...` -> put into `.sops.yaml`
   - Private key: `AGE-SECRET-KEY-1...` -> keep secret on deploy machine
4. Update `.sops.yaml` with your key provider (age/pgp/kms/azure_kv/gcp_kms).
5. Create or re-encrypt the secrets file (prod only):
   ```bash
   dotnet sops encrypt --in-place secrets/appsettings.Production.secrets.json
   ```

## Local development

Run the API using decrypted secrets (dotnet-sops handles decryption):
```bash
dotnet sops run -- dotnet run --project src/UltimateTicTacToe.Api
```

## Production

Use your CI/CD or host environment to decrypt the file before startup, or run the app via:
```bash
dotnet sops run -- dotnet UltimateTicTacToe.Api.dll
```

The API reads the decrypted file only in non-Development environments via `AddEnvironmentSecrets(...)`.

## Rotate age recipient (re-encrypt)

1. Update the `age` recipient in `.sops.yaml`.

### Option A: using `sops` CLI
```bash
sops --rotate --in-place secrets/appsettings.Production.secrets.json
```

### Option B: using `dotnet-sops` only
```bash
dotnet sops decrypt secrets/appsettings.Production.secrets.json > /tmp/appsettings.dec.json
dotnet sops encrypt --in-place /tmp/appsettings.dec.json
mv /tmp/appsettings.dec.json secrets/appsettings.Production.secrets.json
```

Notes:
- Do not commit temporary decrypted files.
- Ensure the new age private key is present on the deploy machine.

## Manual Ubuntu 20.04 deploy (suggested)

1. Copy release output to the server (e.g. `rsync` to `/opt/ultimate-ttt/`).
2. Place the encrypted secrets file next to the app:
   - `/opt/ultimate-ttt/secrets/appsettings.Production.secrets.json`
3. Ensure the decryption key is available on the server (age/pgp/kms).
4. Start the service using dotnet-sops:
   ```bash
   dotnet sops run -- dotnet /opt/ultimate-ttt/UltimateTicTacToe.Api.dll
   ```
5. The final app folder contains the encrypted file; secrets are decrypted at runtime.
