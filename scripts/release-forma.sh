#!/bin/bash

# release-forma.sh - Script per gestire i rilasci ibridi di Forma

# Funzione per mostrare l'utilizzo dello script
show_usage() {
    echo "Utilizzo: $0 -c <component> -v <version>"
    echo ""
    echo "Parametri:"
    echo "  -c, --component  Componente da rilasciare (core, chains, pubsub)"
    echo "  -v, --version    Versione da rilasciare (es. 1.0.0)"
    echo ""
    echo "Esempi:"
    echo "  $0 -c core -v 2.0.0"
    echo "  $0 -c chains -v 1.3.1"
    exit 1
}

# Analisi dei parametri
COMPONENT=""
VERSION=""

while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        -c|--component)
            COMPONENT="$2"
            shift
            shift
            ;;
        -v|--version)
            VERSION="$2"
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
if [ -z "$COMPONENT" ] || [ -z "$VERSION" ]; then
    echo "Errore: I parametri component e version sono obbligatori"
    show_usage
fi

# Verifica che il componente sia valido
if [ "$COMPONENT" != "core" ] && [ "$COMPONENT" != "chains" ] && [ "$COMPONENT" != "pubsub" ]; then
    echo "Errore: Il componente deve essere uno tra: core, chains, pubsub"
    show_usage
fi

# Verifica che il formato della versione sia valido
if ! [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Errore: La versione deve essere nel formato X.Y.Z"
    show_usage
fi

# Verifica che git sia disponibile
if ! command -v git &> /dev/null; then
    echo "Errore: Git non è disponibile nel PATH. Per favore, installa git."
    exit 1
fi

# Verifica che non ci siano modifiche non committate
if [ -n "$(git status --porcelain)" ]; then
    echo "Errore: Ci sono modifiche non committate nel repository."
    echo "Commita o stasha le tue modifiche prima di rilasciare."
    git status --short
    exit 1
fi

# Genera il tag appropriato basato sul componente
TAG="v$VERSION-$COMPONENT"

# Funzione per verificare che il tag non esista già
check_tag_exists() {
    if git tag -l | grep -q "^$TAG$"; then
        echo "Errore: Il tag '$TAG' esiste già. Per favore, usa una versione diversa."
        exit 1
    fi
}

# Verifica se il tag esiste già
check_tag_exists

# Processo di rilascio per componente "core"
if [ "$COMPONENT" = "core" ]; then
    echo "Rilascio del Core con versione $VERSION..."

    # Aggiorna il file Directory.Build.props
    BUILD_PROPS_PATH="Directory.Build.props"
    
    if [ -f "$BUILD_PROPS_PATH" ]; then
        # Usa sed per sostituire la versione - diversa sintassi per macOS/BSD e Linux
        if [ "$(uname)" == "Darwin" ]; then
            # macOS
            sed -i '' "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" "$BUILD_PROPS_PATH"
        else
            # Linux e altri Unix
            sed -i "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" "$BUILD_PROPS_PATH"
        fi
    else
        echo "Errore: File $BUILD_PROPS_PATH non trovato!"
        exit 1
    fi
    
    # Commita l'aggiornamento della versione
    git add "$BUILD_PROPS_PATH"
    git commit -m "Bump version to $VERSION for Core release"
    
    # Crea e pusha il tag
    git tag "$TAG"
    git push origin "$TAG"
    git push

    echo "Tag '$TAG' creato e pushato. Il workflow GitHub Actions dovrebbe attivarsi a breve."

# Processo di rilascio per componenti specifici (chains, pubsub, ecc.)
else
    echo "Rilascio del componente $COMPONENT con versione $VERSION..."

    # Determina il percorso del file csproj in base al componente
    CSPROJ_PATH=""
    if [ "$COMPONENT" = "chains" ]; then
        CSPROJ_PATH="src/Forma.Chains/Forma.Chains.csproj"
    elif [ "$COMPONENT" = "pubsub" ]; then
        CSPROJ_PATH="src/Forma.PubSub.InMemory/Forma.PubSub.InMemory.csproj"
    fi

    if [ ! -f "$CSPROJ_PATH" ]; then
        echo "Errore: File $CSPROJ_PATH non trovato!"
        exit 1
    fi

    # Verifica se esiste già un tag Version nel file
    if grep -q "<Version>.*</Version>" "$CSPROJ_PATH"; then
        # Usa sed per sostituire la versione esistente - diversa sintassi per macOS/BSD e Linux
        if [ "$(uname)" == "Darwin" ]; then
            # macOS
            sed -i '' "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" "$CSPROJ_PATH"
        else
            # Linux e altri Unix
            sed -i "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" "$CSPROJ_PATH"
        fi
    else
        # Se non esiste un tag Version, lo aggiungiamo dopo PropertyGroup
        if [ "$(uname)" == "Darwin" ]; then
            # macOS
            sed -i '' "s/<PropertyGroup>/<PropertyGroup>\\
    <Version>$VERSION<\/Version>/" "$CSPROJ_PATH"
        else
            # Linux e altri Unix
            sed -i "s/<PropertyGroup>/<PropertyGroup>\\n    <Version>$VERSION<\/Version>/" "$CSPROJ_PATH"
        fi
    fi
    
    # Commita l'aggiornamento della versione
    git add "$CSPROJ_PATH"
    git commit -m "Bump version to $VERSION for $COMPONENT release"
    
    # Crea e pusha il tag
    git tag "$TAG"
    git push origin "$TAG"
    git push

    echo "Tag '$TAG' creato e pushato. Il workflow GitHub Actions dovrebbe attivarsi a breve."
fi

echo "Processo di rilascio completato con successo."
