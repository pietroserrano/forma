#!/bin/bash

# test-workflow.sh - Script per testare GitHub Actions localmente con act

# Funzione per mostrare l'utilizzo dello script
show_usage() {
    echo "Utilizzo: $0 -t <workflow_type> [-v <version>] [-c <component>]"
    echo ""
    echo "Parametri:"
    echo "  -t, --type       Tipo di workflow (core o component)"
    echo "  -v, --version    Versione da usare per il test (default: 1.0.0-test)"
    echo "  -c, --component  Componente per workflow component (default: chains)"
    echo ""
    echo "Esempi:"
    echo "  $0 -t core"
    echo "  $0 -t component -c pubsub -v 1.2.3-test"
    exit 1
}

# Analisi dei parametri
WORKFLOW_TYPE=""
VERSION="1.0.0-test"
COMPONENT="chains"

while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        -t|--type)
            WORKFLOW_TYPE="$2"
            shift
            shift
            ;;
        -v|--version)
            VERSION="$2"
            shift
            shift
            ;;
        -c|--component)
            COMPONENT="$2"
            shift
            shift
            ;;
        *)
            echo "Parametro non riconosciuto: $1"
            show_usage
            ;;
    esac
done

# Validazione dei parametri
if [ -z "$WORKFLOW_TYPE" ]; then
    echo "Errore: Il parametro workflow_type è obbligatorio"
    show_usage
fi

if [ "$WORKFLOW_TYPE" != "core" ] && [ "$WORKFLOW_TYPE" != "component" ]; then
    echo "Errore: Il tipo di workflow deve essere 'core' o 'component'"
    show_usage
fi

# Verifica che Docker sia in esecuzione
if ! docker info &> /dev/null; then
    echo "Errore: Docker non è in esecuzione. Avvia Docker e riprova."
    exit 1
fi

# Verifica che act sia installato
if ! command -v act &> /dev/null; then
    echo "Errore: act non è installato. Installa act seguendo le istruzioni in scripts/install-act.md"
    exit 1
fi

# Determina quale workflow testare e imposta i parametri
WORKFLOW_FILE=""
EVENT_NAME="push"
EVENT_FILE=$(mktemp)

if [ "$WORKFLOW_TYPE" = "core" ]; then
    WORKFLOW_FILE=".github/workflows/nuget-deploy.yml"
    TAG_NAME="v${VERSION}-core"
    cat > "$EVENT_FILE" <<EOF
{
  "ref": "refs/tags/$TAG_NAME",
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
EOF
else
    WORKFLOW_FILE=".github/workflows/nuget-component-deploy.yml"
    TAG_NAME="v${VERSION}-${COMPONENT}"
    cat > "$EVENT_FILE" <<EOF
{
  "ref": "refs/tags/$TAG_NAME",
  "repository": {
    "name": "forma",
    "owner": {
      "name": "user"
    }
  }
}
EOF
fi

echo "Testando il workflow di tipo '$WORKFLOW_TYPE' con tag '$TAG_NAME'..."

# Esegui act e simula un evento di push di tag
act_cmd="act push --eventpath $EVENT_FILE -W $WORKFLOW_FILE --secret NUGET_API_KEY=fake-api-key --container-architecture linux/amd64"

# Mostra il comando
echo "Comando act: $act_cmd --dry-run"

# Esegui act con --dry-run per vedere prima cosa verrà eseguito
eval "$act_cmd --dry-run"

read -p "Vuoi procedere con l'esecuzione effettiva? [s/N] " confirmation
if [[ $confirmation =~ ^[Ss]$ ]]; then
    # Esegui act davvero
    eval "$act_cmd"
else
    echo "Esecuzione annullata."
fi

# Rimuovi il file temporaneo
if [ -f "$EVENT_FILE" ]; then
    rm "$EVENT_FILE"
fi
