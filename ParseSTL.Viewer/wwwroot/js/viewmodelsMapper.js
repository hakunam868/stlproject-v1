function initializeDotNetHelper(dotNetHelper, methodNames, jsObjectName) {
    window[jsObjectName] = window[jsObjectName] || {};

    methodNames.forEach(methodName => {
        window[jsObjectName][methodName] = function (...args) {
            return dotNetHelper.invokeMethodAsync(methodName, ...args);
        };
    });
}

// Cached base URL variable
let cachedBaseUrl = null;

// Function to get the MakerBaseUrl from the meta tag, with caching
function getBaseUrl() {
    if (cachedBaseUrl) {
        return cachedBaseUrl; // Return cached base URL if it exists
    }

    // Retrieve base URL from the meta tag if not already cached
    const baseUrlElement = document.querySelector('meta[name="MakerBaseUrl"]');
    cachedBaseUrl = baseUrlElement ? baseUrlElement.getAttribute('content') : '/'; // Default to '/' if not found

    return cachedBaseUrl;
}
// Function to load a single resource (JS or CSS)
function loadResource(filePath, reload = false) {
    var fileElement;
    var fileType;

    // Get the base URL from the custom meta tag
    var baseUrl = getBaseUrl();

    // Prepend baseUrl to filePath
    filePath = baseUrl + filePath;

    // Auto-detect file type based on extension
    if (filePath.endsWith('.js')) {
        fileType = 'js';
    } else if (filePath.endsWith('.css')) {
        fileType = 'css';
    } else {
        throw new Error("Unsupported file type. Only .js and .css files are allowed.");
    }

    // Determine if the file is already loaded
    var existingElement = document.querySelector((fileType === 'js' ? 'script' : 'link') + '[src="' + filePath + '"], ' + (fileType === 'css' ? 'link' : 'script') + '[href="' + filePath + '"]');

    // If reload is true and the file is already loaded, remove the old one
    if (reload && existingElement) {
        existingElement.parentNode.removeChild(existingElement);
        existingElement = null;
    }

    // If the file is not loaded or needs to be reloaded
    if (!existingElement) {
        if (fileType === 'js') {
            // Create a script element for JS files
            fileElement = document.createElement('script');
            fileElement.type = 'text/javascript';
            fileElement.src = filePath;
            fileElement.defer = true; // Optional, use defer for non-blocking JS loading
        } else if (fileType === 'css') {
            // Create a link element for CSS files
            fileElement = document.createElement('link');
            fileElement.rel = 'stylesheet';
            fileElement.href = filePath;
        }

        // Append the new element to the document head
        if (fileElement) {
            document.head.appendChild(fileElement);
            console.log(filePath + " loaded successfully.");
        }
    } else {
        console.log(filePath + " is already loaded.");
    }
}

// Sequentially load an array of resources using the existing loadResource function
function loadResourcesSequentially(resourceArray) {
    // Function to load a script and return a promise that resolves when the script is fully loaded
    const loadScript = (filePath, reload = false) => {
        filePath = `${getBaseUrl()}${filePath}`
        return new Promise((resolve, reject) => {
            // Auto-detect file type based on extension
            var fileType = filePath.endsWith('.js') ? 'script' : 'link';

            // Check if the resource is already loaded and handle the reload flag
            var existingElement = document.querySelector((fileType === 'script' ? 'script' : 'link') + '[src="' + filePath + '"], ' + (fileType === 'link' ? 'link' : 'script') + '[href="' + filePath + '"]');

            if (!reload && existingElement) {
                console.log(filePath + " is already loaded. Skipping.");
                resolve(); // Resolve immediately if already loaded and reload is false
                return;
            }

            // Remove the old element if reload is true
            if (reload && existingElement) {
                existingElement.parentNode.removeChild(existingElement);
                existingElement = null;
            }

            // Create new script/link element and append it to the document head
            let fileElement;
            if (fileType === 'script') {
                fileElement = document.createElement('script');
                fileElement.type = 'text/javascript';
                fileElement.src = filePath;
                fileElement.defer = true; // Optional, for non-blocking JS loading
            } else if (fileType === 'link') {
                fileElement = document.createElement('link');
                fileElement.rel = 'stylesheet';
                fileElement.href = filePath;
            }

            fileElement.onload = () => {
                console.log(filePath + " loaded successfully.");
                resolve(); // Resolve when the script is loaded
            };

            fileElement.onerror = () => {
                console.error("Failed to load " + filePath);
                reject(); // Reject if the script fails to load
            };

            document.head.appendChild(fileElement);
        });
    };

    // Function to load resources sequentially using promises
    const loadNextResource = (index) => {
        if (index < resourceArray.length) {
            const resource = resourceArray[index];
            const { filePath, reload = false } = resource; // Destructure filePath and set reload to false by default

            loadScript(filePath, reload)
                .then(() => loadNextResource(index + 1)) // Load the next script after this one is done
                .catch((err) => console.error(err)); // Catch any errors in the loading process
        }
    };

    // Start loading from the first resource
    loadNextResource(0);
}
// Unload a resource function
function unloadResource(filePath) {
    // Get the base URL from the custom meta tag
    var baseUrl = getBaseUrl();

    // Prepend baseUrl to filePath
    filePath = baseUrl + filePath;

    var fileType;

    // Auto-detect file type based on extension
    if (filePath.endsWith('.js')) {
        fileType = 'js';
    } else if (filePath.endsWith('.css')) {
        fileType = 'css';
    } else {
        throw new Error("Unsupported file type. Only .js and .css files are allowed.");
    }

    // Find the file element in the DOM
    var existingElement = document.querySelector(
        (fileType === 'js' ? 'script' : 'link') +
        '[src="' + filePath + '"], ' +
        (fileType === 'css' ? 'link' : 'script') +
        '[href="' + filePath + '"]'
    );

    // Remove the element from the DOM if found
    if (existingElement) {
        existingElement.parentNode.removeChild(existingElement);
        console.log(filePath + " unloaded successfully.");
    } else {
        console.log(filePath + " not found in the DOM.");
    }
}


window.fileDialog = {
    show: async function (options) {
        if (options.type === "save") {
            // Custom save dialog logic here
            return "selected-save-path";
        } else if (options.type === "open") {
            // Custom open dialog logic here
            return "selected-open-path";
        }
        return "";
    }
};
