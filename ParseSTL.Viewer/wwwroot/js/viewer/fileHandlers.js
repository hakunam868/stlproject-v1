var default_material = new THREE.MeshNormalMaterial({ side: THREE.DoubleSide });
var toastTimeout;

function showToast(message, isError = false) {
    toast.textContent = message;
    toast.classList.toggle("error", isError);
    toast.style.display = "block";
    window.clearTimeout(toastTimeout);
    toastTimeout = window.setTimeout(() => {
        toast.style.display = "none";
        toast.classList.remove("error");
    }, isError ? 6000 : 3000);
}

// Function to show the loader
function showLoader() {
    document.getElementById("loader").classList.remove("hidden");
}

// Function to hide the loader
function hideLoader() {
    document.getElementById("loader").classList.add("hidden");
}

function getFileExtension(filename) {
    return filename.split(".").pop().toLowerCase();
}

async function handleSTLFile(fileData, fitToView = true) {
    const reader = new FileReader();
    reader.onload = () => {
        handleSTLFile_(reader.result, fileData.name, fitToView);
    };
    reader.onerror = () => {
        showToast(`Could not read ${fileData.name}: ${reader.error?.message || "file read failed."}`, true);
    };
    reader.onabort = () => {
        showToast(`Reading ${fileData.name} was cancelled.`, true);
    };
    reader.readAsArrayBuffer(fileData);
}

function handleSTLFile_(byteData, fileName = "STL file", fitToView = true) {
    try {
        const stlPart = new STLPart(byteData);
        renderer.main_group.add(stlPart.mesh);
        if (fitToView) frameView();
        showToast(`Loaded ${fileName}.`);
        return true;
    } catch (error) {
        console.error(`Could not load ${fileName}:`, error);
        showToast(`Could not load ${fileName}: ${error.message || error}`, true);
        return false;
    }
}

function handle3MFFile_(byteData, fileName = "3MF file") {
    try {
        const part_3mf = new Part3MF(byteData);
        let hasRenderableMesh = false;
        part_3mf.mesh.traverse((object) => {
            if (!object.isMesh) return;

            const position = object.geometry?.getAttribute("position");
            if (!position || position.count === 0) return;

            hasRenderableMesh = true;
            for (const coordinate of position.array) {
                if (!Number.isFinite(coordinate)) {
                    throw new Error("3MF file contains a non-finite vertex coordinate.");
                }
            }
        });

        if (!hasRenderableMesh) {
            throw new Error("3MF file contains no renderable mesh.");
        }

        renderer.main_group.add(part_3mf.mesh);
        showToast(`Loaded ${fileName}.`);
        return true;
    } catch (error) {
        console.error(`Could not load ${fileName}:`, error);
        const message = error instanceof Error ? error.message : String(error);
        showToast(`Could not load ${fileName}: ${message}`, true);
        return false;
    }
}

async function handle3MFFile(fileData) {
    const reader = new FileReader();
    reader.onload = () => {
        handle3MFFile_(reader.result, fileData.name);
    };
    reader.onerror = () => {
        showToast(`Could not read ${fileData.name}: ${reader.error?.message || "file read failed."}`, true);
    };
    reader.onabort = () => {
        showToast(`Reading ${fileData.name} was cancelled.`, true);
    };
    reader.readAsArrayBuffer(fileData);
}

var occt;

// this is when the actual renderer is loaded but occt is not ready to load step files
console.log("loading started from occt wasm");
occtimportjs().then(async (occt_) => {
    console.log("buffer received from c#");
    showToast("Ready to load STEP file.");

    occt = occt_;
    loadfromDAM();
});

function loadfromDAM() {
    // Check if window.FilePath is empty
    if (!window.FilePath) {
        return; // Don't call viewerData?.loadFile if window.FilePath is empty
    }

    viewerData?.loadFile(window.FilePath).then((resultBytes) => {
        if (resultBytes?.length) {
            const extension = getFileExtension(window.FilePath);
            console.log("extension: ", extension);
            if (extension === "stl") {
                handleSTLFile_(new Int8Array(resultBytes).buffer);
            } else if (extension === "3mf") {
                handle3MFFile_(new Int8Array(resultBytes).buffer);
            } else {
                handleSTEPFile_(new Int8Array(resultBytes).buffer, extension);
            }
        }
    });
}

function handleSTEPFile_(fileData, format) {
    showLoader();

    try {
        let result;
        switch (format) {
            case "step":
            case "stp":
                result = occt.ReadStepFile(new Uint8Array(fileData), null);
                break;
            case "iges":
            case "igs":
                result = occt.ReadIgesFile(new Uint8Array(fileData), null);
                break;
            case "brep":
            case "brp":
                result = occt.ReadBrepFile(new Uint8Array(fileData), null);
                break;
            default:
                throw new Error(`Unsupported CAD format: ${format}`);
        }

        if (!result || !Array.isArray(result.meshes) || result.meshes.length === 0 || !result.root?.children?.length) {
            throw new Error("The file did not contain a valid CAD model.");
        }

        const new_part = new SolidPart(result.meshes, result.root);
        renderer.main_group.add(new_part.mesh);
        showToast(`Loaded ${format.toUpperCase()} file.`);
        return true;
    } catch (error) {
        console.error(`Could not load ${format.toUpperCase()} file:`, error);
        showToast(`Could not load ${format.toUpperCase()} file: ${error.message || error}`, true);
        return false;
    } finally {
        hideLoader();
    }
}

async function handleSTEPFile(fileData, format) {
    if (occt == undefined) {
        console.log("Please wait... Occt not initialized yet");
        showToast("Please wait... Initializing");
        return;
    }

    const reader = new FileReader();
    reader.onload = () => {
        try {
            handleSTEPFile_(reader.result, format);
        } catch (error) {
            // Keep unexpected callback failures visible and avoid a stuck loader.
            console.error(`Could not load ${fileData.name}:`, error);
            showToast(`Could not load ${fileData.name}: ${error.message || error}`, true);
            hideLoader();
        }
    };
    reader.onerror = () => {
        showToast(`Could not read ${fileData.name}: ${reader.error?.message || "file read failed."}`, true);
    };
    reader.onabort = () => {
        showToast(`Reading ${fileData.name} was cancelled.`, true);
    };
    reader.readAsArrayBuffer(fileData);
}
