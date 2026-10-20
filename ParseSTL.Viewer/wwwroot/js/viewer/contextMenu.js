// The contect menu holder
var ContextMenu = document.getElementById("ContextMenu");
ContextMenu.color = document.getElementById("color");
ContextMenu.opacity = document.getElementById("opacity");
ContextMenu.isolatePart = document.getElementById("isolatePart");

ContextMenu.hide = () => {
    ContextMenu.style.transform = "scale(0.0)";
};
ContextMenu.show = () => {
    ContextMenu.style.transform = "scale(1.0)";
};

// Transparncy slider
var opacity = document.getElementById("opacity");

// The comment from html
var annotate = document.getElementById("annotate");

function showContextMenu() {
    const screenCoordinates = new THREE.Vector3()
        .copy(selection_box[0].point)
        .project(renderer.camera);

    // Convert screen coordinates to pixel values
    const widthHalf = window.innerWidth / 2;
    const heightHalf = window.innerHeight / 2;
    const x = screenCoordinates.x * widthHalf + widthHalf + selection_box[0].offx;
    const y =
        -(screenCoordinates.y * heightHalf) + heightHalf + selection_box[0].offy;

    // ContextMenu.style.left = x + "px";
    // ContextMenu.style.top = y + "px";
    // ContextMenu.style.transform = "scale(1.0)";
}

var draggable = new Draggable(
    document.querySelector(".grab_menu"),
    ContextMenu
);
