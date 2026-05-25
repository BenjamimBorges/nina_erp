#!/bin/bash
set -euo pipefail

echo "🔧 Instalando .NET 8 SDK..."
echo ""

# Detectar distribuição
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$ID
else
    echo "❌ Não conseguiu detectar distribuição Linux"
    exit 1
fi

case "$OS" in
    ubuntu|debian)
        echo "📦 Detectado: Ubuntu/Debian"
        echo "Adicionando repositório Microsoft..."
        
        if [ ! -f /usr/share/keyrings/microsoft.gpg ]; then
            wget -qO /tmp/microsoft.asc https://packages.microsoft.com/keys/microsoft.asc
            sudo gpg --dearmor -o /usr/share/keyrings/microsoft.gpg /tmp/microsoft.asc
        fi
        
        if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ]; then
            echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/microsoft-prod.list > /dev/null
        fi
        
        sudo apt-get update
        sudo apt-get install -y dotnet-sdk-8.0
        ;;
    
    fedora|rhel|centos)
        echo "📦 Detectado: Fedora/RHEL/CentOS"
        sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
        sudo dnf install -y dotnet-sdk-8.0
        ;;
    
    *)
        echo "⚠️  Distribuição não suportada: $OS"
        echo "Visite: https://learn.microsoft.com/dotnet/core/install/linux"
        exit 1
        ;;
esac

echo ""
echo "✅ Instalação concluída!"
echo ""
dotnet --version
echo ""
echo "🎉 .NET SDK instalado com sucesso!"
