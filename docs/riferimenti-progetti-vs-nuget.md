# Guida ai riferimenti condizionali: Progetti vs NuGet

Questo progetto è configurato per utilizzare riferimenti condizionali tra progetti, permettendo di:
- Utilizzare riferimenti diretti ai progetti durante lo sviluppo locale
- Utilizzare pacchetti NuGet quando si compila in un ambiente CI/CD (pipeline di build)

## Come funziona

Il sistema è implementato attraverso la proprietà MSBuild `UseProjectReferences` definita in `Directory.Build.props`. Per default, questa proprietà è impostata a `true`, il che significa che in locale vengono utilizzati i riferimenti diretti ai progetti.

Quando vuoi compilare utilizzando i pacchetti NuGet anziché i riferimenti ai progetti, puoi impostare la proprietà `UseProjectReferences` a `false`.

## Utilizzo in locale

Non c'è bisogno di fare nulla di speciale. I file `.csproj` sono configurati per utilizzare i riferimenti ai progetti per default.

```xml
<!-- Esempio di ItemGroup condizionale in un file .csproj -->
<ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
    <ProjectReference Include="..\Forma.Core\Forma.Core.csproj" />
</ItemGroup>
```

## Utilizzo in CI/CD

Per compilare utilizzando pacchetti NuGet anziché riferimenti ai progetti, è sufficiente passare la proprietà MSBuild durante la compilazione:

```bash
dotnet build -p:UseProjectReferences=false
```

Oppure durante il packaging:

```bash
dotnet pack -p:UseProjectReferences=false
```

## Gestione della versione

La versione dei pacchetti NuGet da utilizzare è definita dalla proprietà `FormaVersion` in `Directory.Build.props`, impostata per default a `1.0.*`.

Questo formato permette di specificare major e minor version fisse, mentre lascia libera la patch version. Il formato `1.0.*` farà sì che venga sempre selezionata l'ultima patch version disponibile dei pacchetti `1.0.x`.

Se hai bisogno di utilizzare una versione specifica, puoi sovrascrivere questa proprietà durante la compilazione:

```bash
dotnet build -p:UseProjectReferences=false -p:FormaVersion=1.2.3
```

## Versioning con Nerdbank.GitVersioning

Il progetto utilizza [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) per gestire automaticamente le versioni dei pacchetti. Questo strumento assegna numeri di versione semantica in base ai commit Git e ai tag, rendendo il processo di versioning completamente automatico.

La configurazione del versioning è definita nel file `version.json` alla radice del repository.