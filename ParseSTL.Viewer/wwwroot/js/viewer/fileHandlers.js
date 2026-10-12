var default_material = new THREE.MeshNormalMaterial({ side: THREE.DoubleSide });
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

async function handleSTLFile(fileData) {
    const reader = new FileReader();
    reader.onload = () => {
        handleSTLFile_(reader.result);
    };
    reader.readAsArrayBuffer(fileData);
}

function handleSTLFile_(byteData) {
    console.log("handleSTLFile",byteData)
    let stl_part = new STLPart(byteData);
    renderer.main_group.add(stl_part.mesh);
}

async function handle3MFFile_(byteData) {
    console.log("handle3MFFile", byteData)
    let part_3mf = new Part3MF(byteData);
    renderer.main_group.add(part_3mf.mesh);
}

async function handle3MFFile(fileData) {
    const reader = new FileReader();
    reader.onload = () => {
        handle3MFFile_(reader.result);
    };
    reader.readAsArrayBuffer(fileData);
}

var occt;

// this is when the actual renderer is loaded but occt is not ready to load step files
console.log("loading started from occt wasm");
occtimportjs().then(async (occt_) => {
    console.log("buffer received from c#");
    toast.textContent = "Ready to load STEP file.";
    toast.style.display = "block";
    setTimeout(function () {
        toast.style.display = "none";
    }, 3000);

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
    console.log("handleSTEPFile", fileData)
    showLoader(); // Show the loader before starting the processing

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
            break;
    }
    console.log(result);
    let new_part = new SolidPart(result.meshes, result.root);
    renderer.main_group.add(new_part.mesh);
    hideLoader();
}

async function handleSTEPFile(fileData, format) {
    if (occt == undefined) {
        console.log("Please wait... Occt not initialized yet");
        toast.textContent = "Please wait... Initializing";

        toast.style.display = "block";
        setTimeout(function () {
            toast.style.display = "none";
        }, 4000); // Hide the toast after 4 seconds
        return;
    }

    const reader = new FileReader();
    reader.onload = () => {
        handleSTEPFile_(reader.result, format);
    };
    reader.readAsArrayBuffer(fileData);
}
