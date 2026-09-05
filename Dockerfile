FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ExpenseTracker.sln ./
COPY ExpenseTracker.API/ExpenseTracker.API.csproj ExpenseTracker.API/
COPY ExpenseTracker.Application/ExpenseTracker.Application.csproj ExpenseTracker.Application/
COPY ExpenseTracker.Domain/ExpenseTracker.Domain.csproj ExpenseTracker.Domain/
COPY ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj ExpenseTracker.Infrastructure/
RUN dotnet restore ExpenseTracker.API/ExpenseTracker.API.csproj

COPY . .
RUN dotnet publish ExpenseTracker.API/ExpenseTracker.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ExpenseTracker.API.dll"]
