# Pet Shop API - .NET 8 Web API

Een uitgebreide .NET 8 Web API-applicatie voor de backend van een dierenwinkel webshop met volledige OAuth2 authenticatie, winkelwagenbeheer, productbeheer, bestellingen en e-mail notificaties.

## 🚀 Functionaliteiten

### Core Functionaliteiten

#### 🔐 Authenticatie en Autorisatie
- **OAuth2 met JWT Bearer tokens** voor gebruikersauthenticatie
- **ASP.NET Core Identity** voor gebruikersbeheer
- **Rolgebaseerde autorisatie** (Admin en Customer rollen)
- **Automatische seeding** van admin gebruiker bij eerste startup

#### 🛒 Winkelwagenbeheer
- **Toevoegen/bijwerken/verwijderen** van producten in winkelwagen
- **Anonieme winkelwagen ondersteuning** met sessie-gebaseerde tracking
- **Automatische samenvoegen** van anonieme en gebruiker winkelwagens na login
- **Real-time winkelwagen aantal** tracking

#### 📦 Productbeheer
- **CRUD operaties** voor Admin gebruikers
- **Zoek en filter functionaliteiten** (naam, categorie, prijsrange)
- **Paginering en sortering** voor grote productlijsten
- **Voorraad beheer** met automatische updates bij bestellingen
- **10 diverse dierenwinkel producten** vooraf geladen

#### 📋 Bestellingen
- **Bestelling plaatsen** vanuit winkelwagen
- **Dynamische top 10** meest bestelde producten
- **Bestelgeschiedenis** per gebruiker
- **Order status management** (Pending, Processing, Shipped, Cancelled)
- **Automatische voorraad bijwerking** bij bestelling

#### 📧 E-mail Notificaties
- **Bevestigings-emails** na bestelling met volledige order details
- **HTML email templates** met professioneel design
- **SMTP configuratie** ondersteuning (Gmail, Outlook, etc.)

### Extra Functionaliteiten

#### 📄 API Documentatie
- **Swagger/OpenAPI** volledig geconfigureerd
- **JWT Bearer authenticatie** geïntegreerd in Swagger UI
- **Gedetailleerde endpoint documentatie**

#### 🧪 Unit Tests
- **Uitgebreide test coverage** voor kritieke services
- **In-memory database** testing met Entity Framework
- **Mock objecten** met Moq framework
- **40+ geautomatiseerde tests** voor ProductService, ShoppingCartService, OrderService en AuthService
- **Integration tests** voor API endpoints
- **Performance tests** voor API response times

#### 🏗️ Architectuur
- **Clean Architecture** met duidelijke scheiding van concerns
- **Repository/Service pattern** implementatie
- **DTO's voor data transfer**
- **Dependency Injection** volledig geconfigureerd

## 🛠️ Technische Stack

- **.NET 8 Web API**
- **Entity Framework Core** met SQLite database
- **ASP.NET Core Identity** voor gebruikersbeheer
- **JWT Bearer Authentication**
- **Swagger/OpenAPI** voor documentatie
- **xUnit + Moq** voor unit testing
- **SMTP** voor email functionaliteit

## 📁 Project Structuur

```
PetShop.API/
├── Controllers/         # API Controllers
│   ├── AuthController.cs
│   ├── ProductsController.cs
│   ├── ShoppingCartController.cs
│   └── OrdersController.cs
├── Data/               # Database Context
│   └── ApplicationDbContext.cs
├── DTOs/               # Data Transfer Objects
│   ├── AuthDto.cs
│   ├── ProductDto.cs
│   ├── ShoppingCartDto.cs
│   └── OrderDto.cs
├── Interfaces/         # Service Interfaces
│   ├── IAuthService.cs
│   ├── IProductService.cs
│   ├── IShoppingCartService.cs
│   ├── IOrderService.cs
│   └── IEmailService.cs
├── Models/             # Entity Models
│   ├── ApplicationUser.cs
│   ├── Product.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── ShoppingCart.cs
│   └── ShoppingCartItem.cs
├── Services/           # Business Logic Services
│   ├── AuthService.cs
│   ├── ProductService.cs
│   ├── ShoppingCartService.cs
│   ├── OrderService.cs
│   └── EmailService.cs
├── Migrations/         # Entity Framework Migrations
└── Program.cs         # Application Entry Point

PetShop.Tests/
├── Services/           # Unit Tests
│   ├── ProductServiceTests.cs
│   └── ShoppingCartServiceTests.cs
```

## 🚀 Installatie en Configuratie

### Vereisten
- .NET 8 SDK
- SQLite (automatisch geïnstalleerd)
- Een SMTP server voor email functionaliteit (optioneel)

### Stappen

1. **Clone het project:**
```bash
git clone <repository-url>
cd petshop
```

2. **Installeer dependencies:**
```bash
cd PetShop.API
dotnet restore
```

3. **Database Setup:**
De database wordt automatisch aangemaakt bij de eerste start met:
- Identity tabellen voor gebruikersbeheer
- Product, Order, ShoppingCart tabellen
- 10 vooraf geladen dierenwinkel producten
- Admin gebruiker (admin@petshop.nl / Admin123!)

4. **Email Configuratie (Optioneel):**
Update `appsettings.Development.json` met je SMTP gegevens:
```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "jouw-email@gmail.com",
    "Password": "jouw-app-wachtwoord",
    "FromEmail": "jouw-email@gmail.com",
    "FromName": "Pet Shop"
  }
}
```

5. **Start de applicatie:**
```bash
dotnet run
```

6. **Open Swagger UI:**
Browse naar: `http://localhost:5103`

## 🧪 Tests Uitvoeren

```bash
cd PetShop.Tests
dotnet test
```

**Test Structuur:**
```
PetShop.Tests/
├── Services/
│   ├── ProductServiceTests.cs      # Product service tests
│   ├── ShoppingCartServiceTests.cs # Shopping cart service tests
│   ├── OrderServiceTests.cs        # Order service tests
│   └── AuthServiceTests.cs         # Authentication service tests
├── Integration/
│   └── PetShopApiIntegrationTests.cs # API endpoint tests
└── Performance/
    └── ApiPerformanceTests.cs      # Performance and load tests
```

**Test Results:**
- ✅ 40+ tests uitgevoerd
- ✅ Unit tests voor alle kritieke services
- ✅ Integration tests voor API endpoints
- ✅ Performance tests voor response times
- ✅ Alle tests geslaagd met uitgebreide coverage

## 📚 API Endpoints

### 🔐 Authenticatie
- `POST /api/auth/login` - Gebruiker inloggen
- `POST /api/auth/register` - Nieuwe gebruiker registreren
- `GET /api/auth/check-email` - Controleer of email bestaat

### 📦 Producten
- `GET /api/products` - Alle producten met filters (zoeken, categorie, prijs)
- `GET /api/products/{id}` - Specifiek product
- `GET /api/products/categories` - Alle categorieën
- `GET /api/products/category/{category}` - Producten per categorie
- `POST /api/products` - Nieuw product aanmaken (Admin)
- `PUT /api/products/{id}` - Product bijwerken (Admin)
- `DELETE /api/products/{id}` - Product verwijderen (Admin)
- `PATCH /api/products/{id}/stock` - Voorraad bijwerken (Admin)

### 🛒 Winkelwagen
- `GET /api/shoppingcart` - Winkelwagen ophalen
- `POST /api/shoppingcart/add` - Product toevoegen
- `PUT /api/shoppingcart/items/{itemId}` - Hoeveelheid bijwerken
- `DELETE /api/shoppingcart/items/{itemId}` - Product verwijderen
- `DELETE /api/shoppingcart/clear` - Winkelwagen legen
- `POST /api/shoppingcart/merge` - Winkelwagens samenvoegen
- `GET /api/shoppingcart/count` - Aantal items ophalen

### 📋 Bestellingen
- `POST /api/orders` - Nieuwe bestelling plaatsen
- `GET /api/orders/{id}` - Specifieke bestelling
- `GET /api/orders` - Alle bestellingen (gepagineerd)
- `GET /api/orders/my-orders` - Mijn bestellingen
- `GET /api/orders/top-products` - Top 10 meest bestelde producten
- `PATCH /api/orders/{id}/status` - Status bijwerken (Admin)
- `POST /api/orders/{id}/cancel` - Bestelling annuleren

## 🔑 Authenticatie Gebruik

### 1. Admin Account
```
Email: admin@petshop.nl
Password: Admin123!
Roles: Admin, Customer
```

### 2. JWT Token
Na inloggen krijg je een JWT token die 24 uur geldig is:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "user-id",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "roles": ["Customer"],
  "expiresAt": "2025-07-07T10:00:00Z"
}
```

### 3. Authorization Header
Voor beschermde endpoints:
```
Authorization: Bearer {your-jwt-token}
```

## 📦 Voorbeeld Producten

De database wordt automatisch gevuld met 10 diverse dierenwinkel producten:

1. **Premium Hondenvoer - Kip & Rijst** (€29.99)
2. **Interactief Kattenspeelgoed - Muizenfeest** (€15.99)
3. **Luxe Vogelkooi - Groot Model** (€89.99)
4. **Aquarium Set - 60 Liter** (€129.99)
5. **Knaagdierenkooi - Hamster Palace** (€45.99)
6. **Hondenspeelgoed - Rope Tug** (€8.99)
7. **Kattenbak - Zelfreinigend** (€199.99)
8. **Vogel Zaadmengsel - Deluxe** (€12.99)
9. **Aquarium Decoratie Set** (€24.99)
10. **Konijnenvoer - Pellets Premium** (€18.99)

## 🔍 Zoek en Filter Voorbeelden

### Producten zoeken:
```
GET /api/products?searchTerm=hond&pageSize=5
GET /api/products?category=Hondenvoer&minPrice=20&maxPrice=50
GET /api/products?sortBy=price&sortDescending=true
```

### Paginering:
```
GET /api/products?pageNumber=2&pageSize=5
```

## 📧 Email Templates

Het systeem stuurt automatisch HTML emails bij:
- **Bevestiging van bestelling** met volledige order details
- **Professional email design** met Pet Shop branding

Voorbeeld email inhoud:
- Persoonlijke begroeting
- Bestelnummer en datum
- Overzicht van bestelde items
- Totaalbedrag
- Verwerkingsinformatie

## 🛡️ Beveiliging

- **JWT tokens** met 256-bit encryption
- **Role-based authorization** (Admin/Customer)
- **Input validatie** op alle endpoints
- **SQL injection bescherming** via Entity Framework
- **HTTPS** alleen in productie
- **CORS** geconfigureerd voor cross-origin requests

## 🔧 Configuratie Opties

### JWT Settings:
```json
{
  "JwtSettings": {
    "Key": "your-secret-key-minimum-256-bits",
    "Issuer": "PetShop.API",
    "Audience": "PetShop.Client",
    "ExpiryInHours": 24
  }
}
```

### Database Connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=petshop_dev.db"
  }
}
```

## 🚀 Deployment

Voor productie deployment:

1. **Update connection string** naar productie database
2. **Configureer SMTP settings** voor email
3. **Enable HTTPS** en update JWT settings
4. **Set environment** naar Production
5. **Run migrations** op productie database

## 📈 Performance Features

- **Database indexen** op vaak gebruikte velden
- **Efficient queries** met Entity Framework
- **Lazy loading** voor gerelateerde data
- **Paginering** voor grote datasets
- **Caching** ready architecture

## 🤝 Contributing

Dit project is gebouwd volgens .NET best practices en is klaar voor uitbreiding:

- Clean Architecture pattern
- SOLID principles
- Comprehensive error handling
- Extensive logging
- Unit test coverage
- API documentation

## 📄 Licentie

Dit project is een demo applicatie voor educatieve doeleinden.

---

**Ontwikkeld met ❤️ voor de Pet Shop community**
