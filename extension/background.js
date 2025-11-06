// Background service worker for Send to Kindle extension

// Listen for extension installation
chrome.runtime.onInstalled.addListener(() => {
  console.log('Send to Kindle extension installed');
});

// Handle messages from popup
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === 'getPageContent') {
    // Get the active tab and extract content
    chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
      if (tabs[0]) {
        chrome.scripting.executeScript({
          target: { tabId: tabs[0].id },
          function: extractPageContent
        }, (results) => {
          if (results && results[0]) {
            sendResponse({ success: true, data: results[0].result });
          } else {
            sendResponse({ success: false, error: 'Failed to extract content' });
          }
        });
      }
    });
    return true; // Keep message channel open for async response
  }
});

// Function to be injected into the page to extract content
// This runs in the browser context where the user is already authenticated,
// so it bypasses paywalls, VPNs, and any login requirements
function extractPageContent() {
  // Wait a moment for any lazy-loaded content
  const waitForContent = () => {
    return new Promise((resolve) => {
      // Give dynamic content a moment to load
      setTimeout(() => {
        resolve();
      }, 500);
    });
  };

  // Extract metadata for better article detection
  const getMetadata = () => {
    const meta = {};

    // Try to get article title from meta tags
    const ogTitle = document.querySelector('meta[property="og:title"]');
    const twitterTitle = document.querySelector('meta[name="twitter:title"]');
    meta.title = ogTitle?.content || twitterTitle?.content || document.title;

    // Try to get author
    const author = document.querySelector('meta[name="author"]');
    const ogAuthor = document.querySelector('meta[property="article:author"]');
    meta.author = author?.content || ogAuthor?.content || '';

    // Try to get description
    const description = document.querySelector('meta[name="description"]');
    const ogDescription = document.querySelector('meta[property="og:description"]');
    meta.description = description?.content || ogDescription?.content || '';

    return meta;
  };

  // Convert relative URLs to absolute URLs to avoid broken links
  const makeUrlsAbsolute = () => {
    const baseUrl = window.location.origin;
    const currentPath = window.location.pathname.substring(0, window.location.pathname.lastIndexOf('/') + 1);

    // Clone the document to avoid modifying the actual page
    const clone = document.documentElement.cloneNode(true);

    // Fix image sources
    clone.querySelectorAll('img[src]').forEach(img => {
      const src = img.getAttribute('src');
      if (src && !src.startsWith('http') && !src.startsWith('data:')) {
        if (src.startsWith('//')) {
          img.setAttribute('src', window.location.protocol + src);
        } else if (src.startsWith('/')) {
          img.setAttribute('src', baseUrl + src);
        } else {
          img.setAttribute('src', baseUrl + currentPath + src);
        }
      }
    });

    // Fix link hrefs
    clone.querySelectorAll('a[href]').forEach(link => {
      const href = link.getAttribute('href');
      if (href && !href.startsWith('http') && !href.startsWith('#')) {
        if (href.startsWith('//')) {
          link.setAttribute('href', window.location.protocol + href);
        } else if (href.startsWith('/')) {
          link.setAttribute('href', baseUrl + href);
        } else {
          link.setAttribute('href', baseUrl + currentPath + href);
        }
      }
    });

    return clone.outerHTML;
  };

  // Synchronous return since we can't use async in injected function
  const metadata = getMetadata();
  const processedHtml = makeUrlsAbsolute();

  return {
    title: metadata.title,
    author: metadata.author,
    description: metadata.description,
    html: processedHtml,
    url: window.location.href,
    timestamp: new Date().toISOString()
  };
}
