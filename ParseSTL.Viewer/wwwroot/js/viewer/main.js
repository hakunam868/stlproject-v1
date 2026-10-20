var container = document.querySelector(".viewer");
var renderer = new Renderer(container);

var shellConfig = document.getElementById("shellConfig");

//initial loading
renderer.camera.zoom = 1; // Reset zoom
var distance = renderer.camera.position.distanceTo(
    new THREE.Vector3(0, 0, 0)
);
renderer.camera.position.set(0, 100, 0);
renderer.camera.lookAt(0, 0, 0);

function loadFileImport(files) {
    for (let i = 0; i < files.length; i++) {
        let file = files[i];
        const fileExtension = file.name.split(".").pop().toLowerCase();
        switch (fileExtension) {
            case "stl":
                handleSTLFile(file, false);
                break;
            case "3mf":
                handle3MFFile(file);
                break;
            case "step":
            case "iges":
            case "brep":
            case "stp":
            case "igs":
            case "brp":
                handleSTEPFile(file, fileExtension);
                break;
            default:
                console.error("Unsupported file format");
                break;
        }
    }
}

function loadFile(files) {
    //clear Previous 3D models
    renderer.main_group.clear();

    while (assemblies.children.length > 2) {
        assemblies.removeChild(assemblies.lastChild);
    }

    for (let i = 0; i < files.length; i++) {
        let file = files[i];
        const fileExtension = file.name.split(".").pop().toLowerCase();
        switch (fileExtension) {
            case "stl":
                handleSTLFile(file);
                break;
            case "3mf":
                handle3MFFile(file);
                break;
            case "step":
            case "iges":
            case "brep":
            case "stp":
            case "igs":
            case "brp":
                handleSTEPFile(file, fileExtension);
                break;
            default:
                console.error("Unsupported file format");
                break;
        }
    }
}

//Drag and drop
renderer.canvas.addEventListener("dragover", (e) => {
    e.preventDefault();
});

renderer.canvas.addEventListener("drop", (e) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    loadFile(files);
    e.stopPropagation();
});

//on click load from local Open button
document.getElementById("loadFileBtn").addEventListener("click", function () {
    document.getElementById("fileInput").click();
});

// Listen for changes in the file Open input
document.getElementById("fileInput").addEventListener("change", function (e) {
    const files = e.target.files;
    loadFile(files);
    e.stopPropagation();
});

//on click load from local Import button
document
    .getElementById("loadFileImport")
    .addEventListener("click", function () {
        document.getElementById("fileImport").click();
    });

// Listen for changes in the file Import input
document.getElementById("fileImport").addEventListener("change", function (e) {
    const files = e.target.files;
    loadFileImport(files);
    e.stopPropagation();
});

renderer.canvas.addEventListener("mousemove", (event) => {
    event.preventDefault();
    renderer.mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
    renderer.mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;
});

var mouse_click_x = 0;
var mouse_click_y = 0;

renderer.canvas.addEventListener("mousedown", (event) => {
    if (event.button === 2) {
        mouse_click_x = event.clientX;
        mouse_click_y = event.clientY;
    }
});

// This is to handle right click callback on 3D Model
renderer.canvas.addEventListener("mouseup", (event) => {
    var intersects;
    if (renderer.mode == 0)
        intersects = renderer.raycaster.intersectObjects(
            renderer.main_group.children
        );
    else if (renderer.mode == 1)
        intersects = renderer.raycaster.intersectObjects(
            renderer.isolated_group.children
        );

    if (intersects.length) {
        if (event.button == 0) {
            deselectAll();
            select(intersects[0].object.userData.shell);
        } else if ((event.button = 2)) {
            // if (ContextMenu.ref)
            // ContextMenu.show();
        }
    } else {
        deselectAll();
        ContextMenu.hide();
    }
});

// renderer.canvas.addEventListener('dblclick', event => {
//     const selection_mode = document.querySelector('input[name="mode"]:checked');
//     if (event.button === 0)
//     {
//         if (selection_mode.value == "shell" && last_highlighted_model)
//         {
//             if (selection_box.length == 1) {
//                 selection_box[0].parent_body.comment = annotate.value
//                 annotate.value = ''
//             }
//             selection_box.forEach(b => renderer.scene.remove(b))
//             selection_box = []
//             ContextMenu.style.transform = "scale(0.0)";

//             renderer.isolated_group.add(last_highlighted_shell.mesh)
//             renderer.mode = 1
//             document.querySelector("#surface").checked = true
//             document.querySelector('.toggle_selection_mode').style.display = "none"
//             document.querySelector('#back').style.display = "block"
//         }
//     }

//     event.preventDefault()
// });

function selection_mode_change() { }

document.querySelectorAll('input[name="mode"]').forEach((elem) =>
    elem.addEventListener("change", (event) => {
        const selection_mode = document.querySelector('input[name="mode"]:checked');

        console.log(event.target.checked);
        if (event.target.checked) {
            if (last_highlighted_model) {
                last_highlighted_model.shells.forEach((shell) => {
                    shell.surfaces.forEach((surface) => {
                        surface.mesh.material.reflectivity = 0;
                        if (selection_mode.value == "assembly")
                            surface.mesh.material = surface.model.mat;
                        else if (selection_mode.value == "shell")
                            surface.mesh.material = surface.shell.mat;
                        else surface.mesh.material = surface.mat;
                    });
                });
            }
        }
    })
);

window.addEventListener("resize", (e) => renderer.resizeCanvas());
renderer.resizeCanvas();

animate();

//Create GUI view method 3d model
var topViewBtn = document.getElementById("topViewBtn");
var frontViewBtn = document.getElementById("frontViewBtn");
var rightViewBtn = document.getElementById("rightViewBtn");
var leftViewBtn = document.getElementById("leftViewBtn");
var backViewBtn = document.getElementById("backViewBtn");
var bottomViewBtn = document.getElementById("bottomViewBtn");

function frameBoundingBox(bounds, viewDirection) {
    if (bounds.isEmpty()) return false;

    const center = bounds.getCenter(new THREE.Vector3());
    const size = bounds.getSize(new THREE.Vector3());
    const halfSize = size.multiplyScalar(0.5);
    const direction = viewDirection.clone().normalize();
    const forward = direction.clone().negate();
    let upSeed = renderer.camera.up.clone().normalize();
    let right = new THREE.Vector3().crossVectors(forward, upSeed);

    if (right.lengthSq() < 1e-12) {
        upSeed.set(0, 0, 1);
        if (Math.abs(forward.dot(upSeed)) > 0.999) upSeed.set(0, 1, 0);
        right.crossVectors(forward, upSeed);
    }

    right.normalize();
    const up = new THREE.Vector3().crossVectors(right, forward).normalize();
    const halfWidth =
        Math.abs(right.x) * halfSize.x +
        Math.abs(right.y) * halfSize.y +
        Math.abs(right.z) * halfSize.z;
    const halfHeight =
        Math.abs(up.x) * halfSize.x +
        Math.abs(up.y) * halfSize.y +
        Math.abs(up.z) * halfSize.z;
    const halfDepth =
        Math.abs(direction.x) * halfSize.x +
        Math.abs(direction.y) * halfSize.y +
        Math.abs(direction.z) * halfSize.z;
    const verticalFov = (renderer.camera.fov * Math.PI) / 180;
    const aspect = renderer.camera.aspect || 1;
    const horizontalFov = 2 * Math.atan(Math.tan(verticalFov / 2) * aspect);
    const distance =
        (halfDepth +
            Math.max(
                halfHeight / Math.tan(verticalFov / 2),
                halfWidth / Math.tan(horizontalFov / 2)
            )) *
        1.1;
    const fitDistance = distance || 1;

    renderer.camera.up.copy(up);
    renderer.camera.position
        .copy(center)
        .addScaledVector(direction, fitDistance);
    renderer.camera.zoom = 1;
    renderer.camera.near = Math.max(
        (fitDistance - halfDepth) * 0.01,
        fitDistance * 1e-6,
        Number.MIN_VALUE
    );
    renderer.camera.far = Math.max(
        fitDistance + halfDepth + size.length(),
        renderer.camera.near * 10
    );
    renderer.camera.lookAt(center);
    renderer.camera.updateProjectionMatrix();
    renderer.orbit_controls.target.copy(center);
    renderer.orbit_controls.update();
    return true;
}

function frameView() {
    const bounds = new THREE.Box3().setFromObject(renderer.main_group);
    const direction = renderer.camera.position
        .clone()
        .sub(renderer.orbit_controls.target);

    if (direction.lengthSq() === 0) direction.set(1, 1, 1);
    return frameBoundingBox(bounds, direction);
}

function setView(direction) {
    const bounds = new THREE.Box3().setFromObject(renderer.main_group);
    frameBoundingBox(bounds, direction.clone().negate());
}

// Function to handle right view
function rightView() {
    var direction = new THREE.Vector3(0, -1, 0);
    setView(direction);
}

// Function to handle left view
function leftView() {
    var direction = new THREE.Vector3(0, 1, 0);
    setView(direction);
}

// Function to handle front view
function frontView() {
    console.log("frontView");
    var direction = new THREE.Vector3(1, 0, 0);
    setView(direction);
}

// Function to handle back view
function backView() {
    var direction = new THREE.Vector3(-1, 0, 0);
    setView(direction);
}

// Function to handle top view
function topView() {
    var direction = new THREE.Vector3(0, 0, -1);
    setView(direction);
}

// Function to handle bottom view
function bottomView() {
    var direction = new THREE.Vector3(0, 0, 1);
    setView(direction);
}

// Handle click Views

topViewBtn.addEventListener("click", function () {
    topView();
    // Handle click for topViewBtn
    console.log("Top View button clicked");
    // Add your logic here
});

frontViewBtn.addEventListener("click", function () {
    frontView();
    // Handle click for frontViewBtn
    console.log("Front View button clicked");
    // Add your logic here
});

rightViewBtn.addEventListener("click", function () {
    rightView();
    console.log("Right View button clicked");
});

leftViewBtn.addEventListener("click", function () {
    leftView();
    console.log("Left View button clicked");
});

backViewBtn.addEventListener("click", function () {
    backView();
    console.log("Back View button clicked");
});

bottomViewBtn.addEventListener("click", function () {
    bottomView();
    console.log("Bottom View button clicked");
});
