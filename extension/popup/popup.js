// Configuration
const CONFIG = {
  localApiUrl: 'http://localhost:7071/api/ConvertToEpub',
  productionApiUrl: 'https://YOUR_FUNCTION_APP.azurewebsites.net/api/ConvertToEpub',
  websiteUrl: 'http://localhost:4321', // Change to production URL when deployed
  productionWebsiteUrl: 'https://YOUR_STATIC_WEB_APP.azurestaticapps.net'
};

let isLocalMode = false;
let currentPageData = null;

// DOM elements
const loggedOutSection = document.getElementById('logged-out');
const loggedInSection = document.getElementById('logged-in');
const contentSection = document.getElementById('content-section');
const loadingSection = document.getElementById('loading');
const titleInput = document.getElementById('title-input');
const authorInput = document.getElementById('author-input');
const sendBtn = document.getElementById('send-btn');
const openWebsiteBtn = document.getElementById('open-website-btn');
const useLocalBtn = document.getElementById('use-local-btn');
const userEmailSpan = document.getElementById('user-email');
const statusMessage = document.getElementById('status-message');

// Initialize
document.addEventListener('DOMContentLoaded', async () => {
  await checkAuthStatus();
  setupEventListeners();
});

function setupEventListeners() {
  openWebsiteBtn.addEventListener('click', () => {
    chrome.tabs.create({ url: CONFIG.websiteUrl });
  });

  useLocalBtn.addEventListener('click', async () => {
    isLocalMode = true;
    await loadPageContent();
  });

  sendBtn.addEventListener('click', sendToKindle);
}

async function checkAuthStatus() {
  try {
    // Check if user is authenticated by checking the website
    const response = await fetch(`${CONFIG.websiteUrl}/api/auth/status`, {
      credentials: 'include'
    });

    if (response.ok) {
      const data = await response.json();
      if (data.authenticated) {
        showLoggedInState(data.email);
        await loadPageContent();
        return;
      }
    }
  } catch (error) {
    console.log('Not authenticated or local API not available');
  }

  showLoggedOutState();
}

function showLoggedOutState() {
  loggedOutSection.classList.remove('hidden');
  loggedInSection.classList.add('hidden');
  contentSection.classList.add('hidden');
}

function showLoggedInState(email) {
  loggedOutSection.classList.add('hidden');
  loggedInSection.classList.remove('hidden');
  userEmailSpan.textContent = email;
  isLocalMode = false;
}

async function loadPageContent() {
  try {
    showLoading();

    // Request page content from background script
    chrome.runtime.sendMessage({ action: 'getPageContent' }, (response) => {
      if (response.success) {
        currentPageData = response.data;
        titleInput.value = currentPageData.title;
        showContentSection();
      } else {
        showError('Failed to extract page content');
      }
    });
  } catch (error) {
    showError('Error loading page content: ' + error.message);
  }
}

async function sendToKindle() {
  if (!currentPageData) {
    showError('No page content available');
    return;
  }

  const title = titleInput.value.trim();
  const author = authorInput.value.trim();

  if (!title) {
    showError('Please enter a title');
    return;
  }

  try {
    showLoading();

    const payload = {
      html: currentPageData.html,
      title: title,
      author: author || 'Unknown',
      sourceUrl: currentPageData.url
    };

    const apiUrl = isLocalMode ? CONFIG.localApiUrl : CONFIG.productionApiUrl;

    // Get function key from storage if not in local mode
    let headers = {
      'Content-Type': 'application/json'
    };

    if (!isLocalMode) {
      const result = await chrome.storage.sync.get(['functionKey']);
      if (result.functionKey) {
        headers['x-functions-key'] = result.functionKey;
      }
    }

    const response = await fetch(apiUrl, {
      method: 'POST',
      headers: headers,
      body: JSON.stringify(payload)
    });

    if (response.ok) {
      const result = await response.json();
      showSuccess(isLocalMode ?
        'EPUB saved to local directory!' :
        'Sent to your Kindle!');
    } else {
      const error = await response.text();
      showError('Failed to convert: ' + error);
    }
  } catch (error) {
    showError('Error: ' + error.message);
  }
}

function showLoading() {
  contentSection.classList.add('hidden');
  loggedOutSection.classList.add('hidden');
  loggedInSection.classList.add('hidden');
  loadingSection.classList.remove('hidden');
}

function showContentSection() {
  loadingSection.classList.add('hidden');
  contentSection.classList.remove('hidden');
  if (isLocalMode) {
    loggedOutSection.classList.remove('hidden');
  } else {
    loggedInSection.classList.remove('hidden');
  }
}

function showSuccess(message) {
  loadingSection.classList.add('hidden');
  contentSection.classList.remove('hidden');
  if (isLocalMode) {
    loggedOutSection.classList.remove('hidden');
  } else {
    loggedInSection.classList.remove('hidden');
  }

  statusMessage.textContent = message;
  statusMessage.className = 'success';
  statusMessage.classList.remove('hidden');

  setTimeout(() => {
    statusMessage.classList.add('hidden');
  }, 5000);
}

function showError(message) {
  loadingSection.classList.add('hidden');
  contentSection.classList.remove('hidden');
  if (isLocalMode) {
    loggedOutSection.classList.remove('hidden');
  } else {
    loggedInSection.classList.remove('hidden');
  }

  statusMessage.textContent = message;
  statusMessage.className = 'error';
  statusMessage.classList.remove('hidden');
}
