# SmartBorrowLK

**Rent Smart. Earn Easy. AI-powered tech rental platform in Sri Lanka.**

SmartBorrowLK is a full-stack, enterprise-grade web application built with **ASP.NET Core MVC (.NET 10)**. It serves as a modern marketplace where users can list, rent, and manage tech gear and equipment safely and efficiently. The platform features robust role-based access control, real-time messaging, AI integrations, and a seamless booking workflow.

---

## 🌟 Key Features

* **Role-Based Access Control**:
  * **Admin**: Access to an administrative dashboard for overseeing the platform.
  * **Vendor**: Can list gear, manage their listings, and handle incoming booking requests.
  * **User**: Can browse listings, book gear, and manage their bookings.
* **Real-Time Communication**: Integrated **Pusher** for a seamless, global WhatsApp-style chat widget that allows users to communicate with vendors in real-time.
* **Smart AI Integrations**: Leverages **Google Gemini AI** for intelligent features across the application.
* **Robust Booking System**: Full workflow for submitting, reviewing, and managing gear booking requests.
* **Secure Authentication**: Built-in cookie-based authentication and secure password hashing using **BCrypt.Net-Next**.
* **Cloud Storage**: Seamless image and file uploads using **Cloudinary**.
* **Modern & Responsive UI**: Designed with **Bootstrap 5**, custom CSS (`site.css`, `chat.css`), and beautiful typography (Google Fonts - Outfit). Features dark mode support.

## 🛠️ Technology Stack

**Backend**
* **Framework**: ASP.NET Core MVC (.NET 10.0)
* **Database**: PostgreSQL
* **ORM**: Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
* **Security**: Cookie Authentication, BCrypt for password hashing

**Frontend**
* **UI Framework**: Bootstrap 5
* **Templating**: Razor Pages / Views (`.cshtml`)
* **Icons**: Bootstrap Icons, Material Symbols
* **Real-Time WebSockets**: Pusher JS Client

**Third-Party Services & Integrations**
* **DotNetEnv**: For secure environment variable management
* **CloudinaryDotNet**: For media storage
* **PusherServer**: For real-time server-side event broadcasting
* **Gemini AI**: AI service integration

---

## 📂 Project Structure

```text
SmartBorrowLK/
├── Controllers/       # Handles incoming HTTP requests (Admin, Auth, Booking, Chat, Listing, Review)
├── Data/              # Entity Framework DbContext (AppDbContext)
├── Models/            # Domain models (User, Listing, Booking, Item, Message, Review)
├── Services/          # Business logic & 3rd party integrations (Auth, Listing, Cloudinary, GeminiAI, etc.)
├── ViewModels/        # Data transfer objects for views
├── Views/             # Razor view templates
├── wwwroot/           # Static assets (CSS, JS, Images, Libs)
├── Program.cs         # Application entry point and DI container configuration
└── SmartBorrowLK.csproj # Project dependencies and settings
```

---

## 🚀 Getting Started

Follow these steps to set up the project locally.

### Prerequisites

* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
* [PostgreSQL](https://www.postgresql.org/download/)
* A [Cloudinary](https://cloudinary.com/) account
* A [Pusher](https://pusher.com/) account
* A Google Gemini API Key

### 1. Clone the repository

```bash
git clone <your-repository-url>
cd SmartBorrowLK
```

### 2. Configure Environment Variables

Create a `.env` file in the root directory (alongside `Program.cs`) and populate it with your credentials:

```env
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=SmartBorrowLK_DB;Username=postgres;Password=your_password"

# Cloudinary
CLOUDINARY_URL=cloudinary://<API_KEY>:<API_SECRET>@<CLOUD_NAME>

# Pusher
PUSHER_APP_ID=your_app_id
PUSHER_KEY=your_key
PUSHER_SECRET=your_secret
PUSHER_CLUSTER=your_cluster
NEXT_PUBLIC_PUSHER_KEY=your_key
NEXT_PUBLIC_PUSHER_CLUSTER=your_cluster

# Gemini AI
GEMINI_API_KEY=your_gemini_api_key
```

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Apply Database Migrations

Ensure your PostgreSQL server is running, then apply the Entity Framework migrations to create the schema:

```bash
dotnet ef database update
```

### 5. Run the Application

```bash
dotnet run
```

The application will start, and you can access it by navigating to `https://localhost:<port>` (usually `5001` or `7000` depending on your launch settings).

---

## 🛡️ License

This project is proprietary and all rights are reserved by SmartBorrow LK.
