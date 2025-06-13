# Gestione del Versioning con Nerdbank.GitVersioning

Questo progetto utilizza [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) per la gestione automatica delle versioni.

## Come funziona

1. Il file `version.json` nella root del repository definisce la versione di base e le regole di versioning
2. Nerdbank.GitVersioning genera automaticamente i numeri di versione basati sul commit history
3. Non è necessario modificare manualmente i numeri di versione nei file .csproj

## Rilascio di nuove versioni

Per rilasciare una nuova versione di un componente:

1. Assicurati di essere sul branch `main`
2. Crea un nuovo tag con il formato `v{version}-{component}`:
   ```
   git tag v1.0.0-chains
   git push origin v1.0.0-chains
   ```
   
   oppure
   
   ```
   git tag v1.0.0-pubsub
   git push origin v1.0.0-pubsub
   ```

3. Il workflow GitHub Actions `nuget-component-deploy.yml` si occuperà del resto

## Incrementare la versione maggiore

Per incrementare la versione maggiore, modifica il numero nel file `version.json`:

```json
{
  "version": "2.0", // Cambiare questo valore
  ...
}
```

## Versioni Pre-release

Per creare una versione pre-release, usa un branch con un nome appropriato o usa la funzionalità di tag pre-release di Nerdbank.GitVersioning.

## In caso di problemi

Consulta la [documentazione ufficiale di Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/index.md).
