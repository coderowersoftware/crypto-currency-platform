FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5083

ENV ASPNETCORE_URLS=http://+:5083

# Set wallet related environment settings
ENV AUTH_JWT_SECRET=39694a3d-967f-44cd-94dd-c77249c1b48e-02c4cd0b-ca45-4afd-af77-05c0a69ddd3e
ENV CCC_WALLET_EMAIL=admin@ccc.com
ENV CCC_WALLET_PASSWORD=ccc@1234
ENV CCC_WALLET_TENANT_ID=0dde86c2-c953-4519-b307-54ea7d69a225
ENV CCC_WALLET_CLIENT_ID=CCC_APP_PLATFORM@0dde86c2-c953-4519-b307-54ea7d69a225.wab
ENV CCC_WALLET_CLIENT_SECRET=3e8bc63e-f50b-4c5d-b7ce-b15def8cee4a

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Transactions/Transactions.csproj", "src/Transactions/"]
RUN dotnet restore "src/Transactions/Transactions.csproj"
COPY . .
WORKDIR "/src/src/Transactions"
RUN dotnet build "Transactions.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Transactions.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Transactions.dll"]
