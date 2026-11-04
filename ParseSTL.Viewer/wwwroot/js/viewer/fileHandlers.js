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

function handle3MFFile_(byteData, fileName = "3MF file", fitToView = true) {
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
        if (fitToView) frameView();
        showToast(`Loaded ${fileName}.`);
        return true;
    } catch (error) {
        console.error(`Could not load ${fileName}:`, error);
        const message = error instanceof Error ? error.message : String(error);
        showToast(`Could not load ${fileName}: ${message}`, true);
        return false;
    }
}

async function handle3MFFile(fileData, fitToView = true) {
    const reader = new FileReader();
    reader.onload = () => {
        handle3MFFile_(reader.result, fileData.name, fitToView);
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
var occtReady = occtimportjs().then((occt_) => {
    occt = occt_;
    showToast("Ready to load STEP file.");
    loadfromDAM();
    return occt_;
}, (error) => {
    console.error("Could not initialize the STEP importer:", error);
    showToast("Could not initialize the STEP importer.", true);
    return null;
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

function handleSTEPFile_(fileData, format, fitToView = true) {
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

        for (const mesh of result.meshes) {
            const positions = mesh.attributes?.position?.array;
            const indices = mesh.index?.array;
            if (!positions?.length || !indices?.length || indices.length % 3 !== 0) {
                throw new Error("The CAD model contains invalid mesh data.");
            }
            if (positions.some((coordinate) => !Number.isFinite(coordinate))) {
                throw new Error("The CAD model contains a non-finite vertex coordinate.");
            }
            if (!Array.isArray(mesh.brep_faces) || mesh.brep_faces.length === 0) {
                throw new Error("The CAD model contains a mesh without faces.");
            }
        }

        const new_part = new SolidPart(result.meshes, result.root);
        renderer.main_group.add(new_part.mesh);
        if (fitToView) frameView();
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

async function handleSTEPFile(fileData, format, fitToView = true) {
    if (!(await occtReady)) {
        showToast("STEP import is unavailable because the importer failed to initialize.", true);
        return;
    }

    const reader = new FileReader();
    reader.onload = () => {
        try {
            handleSTEPFile_(reader.result, format, fitToView);
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

function exportSTL(group) {
    const meshes = [];
    const groups = Array.isArray(group) ? group : [group];
    for (const root of groups) {
        root.updateWorldMatrix(true, true);
        root.traverse((object) => {
            if (!object.isMesh || !object.geometry?.getAttribute("position")) return;

            let visible = true;
            for (let ancestor = object; ancestor && ancestor !== root; ancestor = ancestor.parent) {
                if (!ancestor.visible) {
                    visible = false;
                    break;
                }
            }
            if (visible) meshes.push(object);
        });
    }

    let triangleCount = 0;
    for (const mesh of meshes) {
        const position = mesh.geometry.getAttribute("position");
        const count = mesh.geometry.index ? mesh.geometry.index.count : position.count;
        if (count % 3 !== 0) {
            throw new Error("A model mesh contains an incomplete triangle.");
        }
        triangleCount += count / 3;
    }

    if (triangleCount === 0) {
        throw new Error("There are no visible model triangles to export.");
    }
    if (triangleCount > 0xffffffff || triangleCount > Math.floor((Number.MAX_SAFE_INTEGER - 84) / 50)) {
        throw new Error("The model is too large to export as an STL file.");
    }

    const buffer = new ArrayBuffer(84 + triangleCount * 50);
    const bytes = new Uint8Array(buffer);
    const header = new TextEncoder().encode("Generated by MAK3R 3D Viewer");
    bytes.set(header.subarray(0, 80));

    const view = new DataView(buffer);
    view.setUint32(80, triangleCount, true);
    let offset = 84;

    const a = new THREE.Vector3();
    const b = new THREE.Vector3();
    const c = new THREE.Vector3();
    const edge1 = new THREE.Vector3();
    const edge2 = new THREE.Vector3();
    const normal = new THREE.Vector3();

    for (const mesh of meshes) {
        const geometry = mesh.geometry;
        const position = geometry.getAttribute("position");
        const index = geometry.index;
        mesh.updateWorldMatrix(true, false);

        for (let triangle = 0; triangle < (index ? index.count : position.count); triangle += 3) {
            const first = index ? index.getX(triangle) : triangle;
            const second = index ? index.getX(triangle + 1) : triangle + 1;
            const third = index ? index.getX(triangle + 2) : triangle + 2;
            if (first >= position.count || second >= position.count || third >= position.count) {
                throw new Error("A model mesh contains an invalid triangle index.");
            }

            a.fromBufferAttribute(position, first).applyMatrix4(mesh.matrixWorld);
            b.fromBufferAttribute(position, second).applyMatrix4(mesh.matrixWorld);
            c.fromBufferAttribute(position, third).applyMatrix4(mesh.matrixWorld);
            if (![a.x, a.y, a.z, b.x, b.y, b.z, c.x, c.y, c.z].every(Number.isFinite)) {
                throw new Error("A model mesh contains a non-finite vertex coordinate.");
            }

            normal.crossVectors(edge1.subVectors(b, a), edge2.subVectors(c, a)).normalize();
            for (const component of [normal.x, normal.y, normal.z, a.x, a.y, a.z, b.x, b.y, b.z, c.x, c.y, c.z]) {
                view.setFloat32(offset, component, true);
                offset += 4;
            }
            view.setUint16(offset, 0, true);
            offset += 2;
        }
    }

    const url = URL.createObjectURL(new Blob([buffer], { type: "model/stl" }));
    const link = document.createElement("a");
    link.href = url;
    link.download = "model.stl";
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    showToast("STL exported.");
}
