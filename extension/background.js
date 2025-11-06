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
function extractPageContent() {
  return {
    title: document.title,
    html: document.documentElement.outerHTML,
    url: window.location.href
  };
}
