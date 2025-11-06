# 📚 Send to Kindle

A complete solution for sending web articles to your Kindle device or app. This project includes a browser extension (Chrome/Edge), a web application, and Azure Functions backend for converting web pages to EPUB format and delivering them to your Kindle.

## 🌟 Features

- **Browser Extension**: One-click sending from any webpage (Chrome & Edge compatible)
- **Web Application**: User account management and Kindle email configuration
- **EPUB Conversion**: Automatically converts web articles to beautiful EPUB files
- **Direct to Kindle**: Sends EPUBs directly to your Kindle email address
- **Google Authentication**: Secure login using Google OAuth
- **Local Testing Mode**: Test without authentication for development

## 📁 Project Structure

```
hive-reader-sharp/
├── extension/              # Chrome/Edge browser extension
│   ├── manifest.json
│   ├── background.js
│   └── popup/
│       ├── popup.html
│       ├── popup.css
│       └── popup.js
├── web/                    # Astro website
│   ├── src/
│   │   ├── pages/         # Routes and API endpoints
│   │   ├── layouts/       # Page layouts
│   │   ├── lib/          # Authentication & database logic
│   │   └── types/        # TypeScript types
│   ├── astro.config.mjs
│   └── package.json
└── functions/              # Azure Functions (C#)
    └── SendToKindle/
        ├── Functions/     # HTTP triggers
        ├── Services/      # EPUB conversion & email
        └── Models/        # Data models
```

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ and npm
- .NET 8.0 SDK
- Azure Functions Core Tools
- Azure account (for deployment)
- Google Cloud Console account (for OAuth)

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/hive-reader-sharp.git
cd hive-reader-sharp
```

### 2. Set Up the Web Application

#### Install Dependencies

```bash
cd web
npm install
```

#### Configure Environment Variables

Create a `.env` file in the `web` directory:

```env
# Google OAuth Configuration
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret
GOOGLE_REDIRECT_URI=http://localhost:4321/api/auth/callback

# Azure Cosmos DB Configuration
COSMOS_ENDPOINT=https://your-cosmos-account.documents.azure.com:443/
COSMOS_KEY=your_cosmos_key

# Node Environment
NODE_ENV=development
```

#### Get Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API
4. Go to Credentials → Create Credentials → OAuth 2.0 Client ID
5. Configure consent screen
6. Add authorized redirect URI: `http://localhost:4321/api/auth/callback`
7. Copy Client ID and Client Secret to `.env`

#### Set Up Azure Cosmos DB

1. Create a Cosmos DB account in Azure Portal
2. Choose Core (SQL) API
3. Copy the endpoint and primary key
4. Add them to `.env` file

#### Run the Web App

```bash
npm run dev
```

The app will be available at `http://localhost:4321`

### 3. Set Up Azure Functions

#### Configure Local Settings

Edit `functions/SendToKindle/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "LOCAL_OUTPUT_DIR": "./output",
    "SMTP_HOST": "smtp.gmail.com",
    "SMTP_PORT": "587",
    "SMTP_USERNAME": "your-email@gmail.com",
    "SMTP_PASSWORD": "your-app-specific-password",
    "FROM_EMAIL": "your-email@gmail.com",
    "FROM_NAME": "Send to Kindle"
  }
}
```

#### Set Up Email (Gmail)

1. Go to Google Account settings
2. Enable 2-factor authentication
3. Generate an App-Specific Password
4. Use this password in `local.settings.json`

#### Run Azure Functions Locally

```bash
cd functions/SendToKindle
dotnet build
func start
```

Functions will be available at `http://localhost:7071`

### 4. Install Browser Extension

#### Chrome/Edge Installation

1. Open Chrome/Edge and navigate to `chrome://extensions`
2. Enable "Developer mode" (toggle in top right)
3. Click "Load unpacked"
4. Select the `extension` folder from this project
5. The extension should now appear in your browser

#### Configure Extension

The extension will automatically detect if you're logged into the web app. For production use:

1. Update `extension/popup/popup.js` with your deployed URLs:
   ```javascript
   const CONFIG = {
     localApiUrl: 'http://localhost:7071/api/convert-local',
     productionApiUrl: 'https://YOUR_FUNCTION_APP.azurewebsites.net/api/convert',
     websiteUrl: 'http://localhost:4321',
     productionWebsiteUrl: 'https://YOUR_STATIC_WEB_APP.azurestaticapps.net'
   };
   ```

## 🧪 Testing Locally

### Local Mode (No Authentication)

The extension includes a "Local Mode" that saves EPUBs to your local machine without requiring authentication:

1. Open any article in your browser
2. Click the Send to Kindle extension icon
3. Click "Use Local Mode"
4. Edit the title if needed
5. Click "Send to Kindle"
6. EPUB will be saved to `functions/SendToKindle/output/`

### Production Mode (With Authentication)

1. Sign in to the web app at `http://localhost:4321`
2. Configure your Kindle email in the dashboard
3. Click the extension icon on any article
4. Edit the title and click "Send to Kindle"
5. Article will be sent to your Kindle email

## 🔒 Kindle Email Setup

### Find Your Kindle Email

1. Go to [Amazon Manage Your Content and Devices](https://www.amazon.com/mycd)
2. Click on "Devices" tab
3. Select your Kindle device
4. Your Kindle email is shown (e.g., `username@kindle.com`)

### Approve Sender Email

1. In the same Amazon page, go to "Preferences" tab
2. Scroll to "Personal Document Settings"
3. Under "Approved Personal Document E-mail List", add the email you're using to send (FROM_EMAIL from Azure Functions config)

## 🚀 Deployment to Azure

### Deploy Azure Functions

#### Create Function App

```bash
az functionapp create \
  --resource-group YourResourceGroup \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --name your-function-app-name \
  --storage-account yourstorageaccount
```

#### Configure Application Settings

```bash
az functionapp config appsettings set \
  --name your-function-app-name \
  --resource-group YourResourceGroup \
  --settings \
    SMTP_HOST=smtp.gmail.com \
    SMTP_PORT=587 \
    SMTP_USERNAME=your-email@gmail.com \
    SMTP_PASSWORD=your-app-password \
    FROM_EMAIL=your-email@gmail.com \
    FROM_NAME="Send to Kindle"
```

#### Deploy

```bash
cd functions/SendToKindle
func azure functionapp publish your-function-app-name
```

### Deploy Static Web App

#### Create Static Web App

1. Go to Azure Portal
2. Create new Static Web App
3. Connect to your GitHub repository
4. Set build configuration:
   - App location: `/web`
   - Output location: `dist`

#### Configure Environment Variables

In Azure Portal, go to Static Web App → Configuration → Environment Variables:

```
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret
GOOGLE_REDIRECT_URI=https://YOUR_APP.azurestaticapps.net/api/auth/callback
COSMOS_ENDPOINT=your_cosmos_endpoint
COSMOS_KEY=your_cosmos_key
NODE_ENV=production
```

### Update Extension URLs

Update `extension/popup/popup.js` with your production URLs and reload the extension.

## 📖 Usage

### Sending an Article

1. **Navigate to any article** you want to read on your Kindle
2. **Click the extension icon** in your browser toolbar
3. **Edit the title** if needed (pre-filled with page title)
4. **Optionally add an author name**
5. **Click "Send to Kindle"**
6. **Wait for confirmation** - the article will be sent to your Kindle email
7. **Check your Kindle** - the article should appear within minutes

### Managing Settings

1. **Sign in** to the web app
2. **Go to Dashboard**
3. **Update your Kindle email** address
4. **Save settings**

## 🔧 Development

### Web App Development

```bash
cd web
npm run dev
```

### Azure Functions Development

```bash
cd functions/SendToKindle
func start
```

### Extension Development

After making changes to extension files:
1. Go to `chrome://extensions`
2. Click the refresh icon on your extension
3. Changes will be applied

## 🛠️ Tech Stack

- **Frontend**: Astro, TypeScript, HTML/CSS
- **Backend**: Azure Functions (C#), .NET 8
- **Database**: Azure Cosmos DB
- **Authentication**: Google OAuth 2.0
- **Email**: MailKit (SMTP)
- **EPUB Generation**: Custom C# implementation with HtmlAgilityPack
- **Hosting**: Azure Static Web Apps, Azure Functions

## 📝 API Endpoints

### Web App API

- `GET /api/auth/login` - Initiate Google OAuth login
- `GET /api/auth/callback` - OAuth callback
- `POST /api/auth/logout` - Sign out
- `GET /api/auth/status` - Check authentication status
- `POST /api/user/update-kindle` - Update Kindle email

### Azure Functions API

- `POST /api/convert-local` - Convert to EPUB and save locally (no auth)
- `POST /api/convert` - Convert to EPUB and send to Kindle (function key auth)

## 🔐 Security

- Function keys required for production API calls
- Google OAuth for user authentication
- HttpOnly cookies for session management
- CORS configuration for extension access
- Approved email list required for Kindle delivery

## 🐛 Troubleshooting

### Extension Not Working

1. Check if you're logged into the web app
2. Verify the API URLs in `popup.js` are correct
3. Check browser console for errors
4. Ensure CORS is configured properly

### Email Not Sending

1. Verify SMTP credentials are correct
2. Check if app-specific password is being used (not regular password)
3. Ensure sender email is approved in Amazon Kindle settings
4. Check Azure Functions logs for errors

### EPUB Not Generating

1. Check Azure Functions logs
2. Verify HTML content is being captured correctly
3. Test with local mode first
4. Ensure .NET 8 SDK is installed

### Cosmos DB Connection Issues

1. Verify endpoint and key are correct
2. Check if database and containers are created
3. Ensure network access is allowed from your IP

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📧 Support

For issues and questions, please open an issue on GitHub.

## 🙏 Acknowledgments

- EPUB format based on IDPF standards
- HTML cleaning powered by HtmlAgilityPack
- Email sending via MailKit

---

**Happy Reading! 📚✨**