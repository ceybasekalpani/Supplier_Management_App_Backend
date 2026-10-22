FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TeaFactorySupplierManagement.API/TeaFactorySupplierManagement.API.csproj", "TeaFactorySupplierManagement.API/"]
COPY ["TeaFactorySupplierManagement.Application/TeaFactorySupplierManagement.Application.csproj", "TeaFactorySupplierManagement.Application/"]
COPY ["TeaFactorySupplierManagement.Domain/TeaFactorySupplierManagement.Domain.csproj", "TeaFactorySupplierManagement.Domain/"]
COPY ["TeaFactorySupplierManagement.Infrastructure/TeaFactorySupplierManagement.Infrastructure.csproj", "TeaFactorySupplierManagement.Infrastructure/"]

RUN dotnet restore "TeaFactorySupplierManagement.API/TeaFactorySupplierManagement.API.csproj"

COPY . .
RUN dotnet publish "TeaFactorySupplierManagement.API/TeaFactorySupplierManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "TeaFactorySupplierManagement.API.dll"]
