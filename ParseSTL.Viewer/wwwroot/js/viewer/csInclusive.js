(function () {
  const browserFetch =
    window.__viewerBrowserFetch || window.fetch.bind(window);
  window.__viewerBrowserFetch = browserFetch;

  window.fetch = function (resource, options) {
    const url =
      typeof resource === "string"
        ? resource
        : resource instanceof URL
          ? resource.href
          : resource?.url;
    if (typeof url !== "string") {
      return browserFetch(resource, options);
    }

    const filename = url.split(/[?#]/, 1)[0].split(/[\\/]/).pop();

    if (filename !== "occt-import-js.wasm" || !window.viewerData?.jsFetch) {
      return browserFetch(resource, options);
    }

    return window.viewerData.jsFetch(url).then((result) => {
      const blob = new Blob([new Uint8Array(result).buffer], {
        type: "application/wasm",
      });

      return new Response(blob, {
        status: 200,
        statusText: "OK",
        headers: {
          "Content-Type": "application/wasm",
        },
      });
    });
  };
})();
