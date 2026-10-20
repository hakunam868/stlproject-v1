// THIS File contains all code which is inclusive to C# calls

function fetch(filename) {
  return new Promise((resolve, reject) => {
    viewerData
      ?.jsFetch(filename)
        .then((result) => {
            console.log("jsFetch",result);
        let blob = new Blob([new Uint8Array(result).buffer], {
          type: "application/wasm",
        });

        const response = new Response(blob, {
          status: 200, // Adjust as needed
          statusText: "OK", // Adjust as needed
          headers: {
            // Add any headers you need
            "Content-Type": "application/wasm", // Adjust as needed
          },
        });

        resolve(response);
      })
      .catch((err) => {
        reject(err);
      });
  });
}
