var Mira3D = undefined;
var toast = document.getElementById("toastMessage");

var last_highlighted_surface = null;
var last_highlighted_shell = null;
var last_highlighted_model = null;
var last_highlighted_mesh = null;
var hover_box = null;
var selection_box = [];

document.getElementById("capture").addEventListener("click", function () {
    renderer.render();

    if (viewerData) {
        // FOR WPF
        renderer.renderer.domElement.toBlob((blob) => {
            blob.arrayBuffer().then((buff) => {
                let view = new Uint8Array(buff);

                const regularArray = [];

                // Copy each element from the Uint8Array to the regular array
                for (let i = 0; i < view.length; i++) {
                    regularArray.push(view[i]);
                }

                viewerData?.saveCapture(regularArray);
            });

            toast.textContent = "Image captured! Please check the Downloads folder.";
            toast.style.display = "block";
            setTimeout(function () {
                toast.style.display = "none";
            }, 4000); // Hide the toast after 4 seconds
        });
    } else {
        // FOR BROWSER
        var dataURL = renderer.canvas.toDataURL("image/png");

        var blob = dataURItoBlob(dataURL);

        var url = window.URL.createObjectURL(blob);

        var link = document.createElement("a");
        link.href = url;
        link.download = "canvas_image.png";

        link.click();

        function dataURItoBlob(dataURI) {
            var byteString = atob(dataURI.split(",")[1]);
            var ab = new ArrayBuffer(byteString.length);
            var ia = new Uint8Array(ab);
            for (var i = 0; i < byteString.length; i++) {
                ia[i] = byteString.charCodeAt(i);
            }
            return new Blob([ab], { type: "image/png" });
        }
    }
});
