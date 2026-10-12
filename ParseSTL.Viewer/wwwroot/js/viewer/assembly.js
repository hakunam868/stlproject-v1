function getRandomFloat() {
    const randomBuffer = new Uint32Array(1);
    window.crypto.getRandomValues(randomBuffer);
    return randomBuffer[0] / (0xffffffff + 1);
}

//var assemblies = createAccordion({ title: "Parts list", color: { r: 0, g: 0, b: 0 } })

document.getElementById("assemblies");
document.addEventListener("contextmenu", function (event) {
    event.preventDefault();
});

function deselectAll() {
    // Deselect in case different item was selected
    if (ContextMenu.ref) {
        // renderer.overlay_scene.remove(ContextMenu.ref.mesh);
        // renderer.scene.add(ContextMenu.ref.mesh);
        renderer.overlay_scene.remove(ContextMenu.ref.selection);
        ContextMenu.ref.selection = undefined;
        ContextMenu.ref.accordion.classList.toggle("active");
        ContextMenu.ref = undefined;
    }
}

function select(ref) {
    if (ref.selection == undefined) {
        // Hiding contect menu because different item was selected
        ContextMenu.hide();
        const boundingBox = new THREE.Box3().setFromObject(ref.mesh);
        ref.selection = new THREE.Box3Helper(boundingBox, 0xff7799);
        // renderer.scene.remove(ref.mesh);
        // renderer.overlay_scene.add(ref.mesh);
        renderer.overlay_scene.add(ref.selection);

        ref.selection.parent_body = ref;
        ContextMenu.ref = ref;
        ContextMenu.ref.accordion.classList.toggle("active");
        ContextMenu.opacity.value = ref.mat.transmission * 100;
        ContextMenu.color.value = rgbToHex(ref.mat.color);
    } else {
        ContextMenu.hide();
    }
}

function createAccordion(item) {
    const accordionItem = document.createElement("div");
    accordionItem.classList.add("accordion");
    accordionItem.style.overflow = "hidden";
    accordionItem.style.maxHeight = "28px";

    // Reference of assembly/part/surface
    accordionItem.ref = item.ref;

    // Create a span element for the title and assigned it to accordionItem.titleSpan
    accordionItem.titleSpan = document.createElement("span");
    accordionItem.titleSpan.classList.add("accordion-title");
    accordionItem.titleSpan.textContent = item.title;
    accordionItem.appendChild(accordionItem.titleSpan);

    // Create arrow icon
    accordionItem.arrow = document.createElement("span");
    accordionItem.arrow.classList.add("arrow");
    accordionItem.arrow.classList.add("arrow-parent");
    accordionItem.appendChild(accordionItem.arrow);

    //Create eye image icon (Show/Hide controls when the user clicks on Eye)
    accordionItem.imgEye = document.createElement("img");
    accordionItem.imgEye.classList.add("image_3d_icon");
    accordionItem.imgEye.classList.add("imageEyeShowHide");
    accordionItem.imgEye.src = `${getBaseUrl()}images/view.png`;
    accordionItem.imgEye.height = "20";
    accordionItem.imgEye.width = "20";
    accordionItem.appendChild(accordionItem.imgEye);

    // Create image icon
    const img3DIcon = document.createElement("img");
    img3DIcon.classList.add("image_3d_icon");
    img3DIcon.src = item.img;
    img3DIcon.height = "20";
    img3DIcon.width = "20";
    accordionItem.appendChild(img3DIcon);

    //Create div for color
    if (item.color) {
        const coloredBlock = document.createElement("div");
        coloredBlock.classList.add("color-container");
        coloredBlock.style.backgroundColor =
            "rgb(" +
            item.color.r * 255 +
            ", " +
            item.color.g * 255 +
            ", " +
            item.color.b * 255 +
            ")";
        accordionItem.appendChild(coloredBlock);
    }

    const hover_in = (event) => {
        // if (accordionItem.style.maxHeight != "max-content") {
        //   if (accordionItem.ref) {
        //     const boundingBox = new THREE.Box3().setFromObject(
        //       accordionItem.ref.mesh
        //     );
        //     accordionItem.ref.hover = new THREE.Box3Helper(
        //       boundingBox,
        //       0x000000
        //     );
        //     renderer.scene.add(accordionItem.ref.hover);
        //     accordionItem.ref.hover.parent_body = accordionItem.ref;
        //   }
        // }
    };

    const hover_out = (event) => {
        // if (accordionItem.style.maxHeight != "max-content") {
        //   // checking if the thing is not selected then only run the hover hover
        //   // UNSELECT THE PART HERE
        //   if (accordionItem.ref) {
        //     if (accordionItem.ref.hover) {
        //       renderer.scene.remove(accordionItem.ref.hover);
        //       accordionItem.ref.hover = undefined;
        //     }
        //   }
        // }
    };

    accordionItem.addEventListener("mouseenter", hover_in);
    accordionItem.addEventListener("mouseleave", hover_out);

    accordionItem.addEventListener("mousedown", (event) => {
        if (event.button != 0) return;
        event.preventDefault();
        event.stopPropagation();
    });

    accordionItem.addEventListener("mouseup", (event) => {
        event.preventDefault();

        if (event.button == 0 || event.button == 2) {
            deselectAll();
            select(accordionItem.ref);
        }
        if (event.button == 2) {
            if (ContextMenu.ref == accordionItem.ref) {
                if (accordionItem.arrow.classList.contains("rotate-arrow"))
                    accordionItem.arrow.click();
                let rect = ContextMenu.ref.accordion.getBoundingClientRect();
                color.value = rgbToHex(ContextMenu.ref.mat.color);
                ContextMenu.show();
                ContextMenu.style.top = rect.top + "px";
                ContextMenu.style.left = rect.right + "px";
            }
        }

        event.stopPropagation();
    });

    // Add click event to toggle children
    accordionItem.arrow.addEventListener("mouseup", function (event) {
        if (event.button != 0) return;
        event.preventDefault();

        accordionItem.arrow.classList.toggle("rotate-arrow");
        if (accordionItem.style.maxHeight == "max-content") {
            accordionItem.style.maxHeight = "28px";
            hover_in(event);
        } else {
            hover_out(event);
            accordionItem.style.maxHeight = "max-content";
        }

        // to call the callback for lowest emement in the hirarchy and not forward the callback to upper elements
        event.stopPropagation();
    });

    // Add click event to toggle accordionItem.imgEye
    accordionItem.imgEye.addEventListener("mouseup", function (event) {
        if (event.button != 0) return;
        event.preventDefault();

        if (accordionItem.ref.type == "Part") {
            accordionItem.ref.mesh.visible = !accordionItem.ref.mesh.visible;
        } else if (accordionItem.ref.type == "Assembly") {
            if (accordionItem.ref.mesh.visible) {
                accordionItem.ref.mesh.visible = false;
            } else {
                accordionItem.ref.shells.forEach((shell) => {
                    shell.mesh.visible = true;
                    shell.accordion.imgEye.src = `${getBaseUrl()}images/view.png`;
                });
                accordionItem.ref.mesh.visible = true;
            }
        }
        if (accordionItem.imgEye.src.endsWith("/images/view.png")) {
            console.log("accordionItem.imgEye in");
            accordionItem.imgEye.src = `${getBaseUrl()}images/hide.png`;
        } else {
            accordionItem.imgEye.src = `${getBaseUrl()}images/view.png`;
        }

        event.stopPropagation();
    });

    return accordionItem;
}

//Isolate part
ContextMenu.isolatePart.addEventListener("click", function (event) {
    event.preventDefault();
    event.stopPropagation();

    // Check if already in isolate mode
    if (renderer.mode === 1) {
        // If already isolated, show all parts
        showAllPartsInAccordion(assemblies);
        renderer.isolated_group.remove(ContextMenu.ref.mesh);
        ContextMenu.ref.model.mesh.add(ContextMenu.ref.mesh);
        //renderer.main_group.add(ContextMenu.ref.mesh);
        renderer.mode = 0;
        isolatePartText.innerText = "Isolate";
        // assemblies.classList.toggle("hidden");
    } else {
        // Isolate Part
        if (ContextMenu.ref.type == "Part") {
            hideAllPartsInAccordionExceptSelected(assemblies, ContextMenu.ref);
            renderer.isolated_group.add(ContextMenu.ref.mesh);
            renderer.mode = 1;
            isolatePartText.innerText = "Show All";
            // assemblies.classList.toggle("hidden");
        }
    }
});

// Function to hide all parts in the accordion treeview except the selected one
function hideAllPartsInAccordionExceptSelected(accordion, selectedRef) {
    const items = accordion.children;
    for (let i = 0; i < items.length; i++) {
        const item = items[i];
        if (item.ref && item.ref.type === "Part") {
            if (item.ref !== selectedRef) {
                item.style.display = "none";
            }
        } else if (item.ref && item.ref.type === "Assembly") {
            hideAllPartsInAccordionExceptSelected(item, selectedRef);
        }
    }
}

// Function to show all parts in the accordion treeview
function showAllPartsInAccordion(accordion) {
    const items = accordion.children;
    for (let i = 0; i < items.length; i++) {
        const item = items[i];
        if (item.ref && item.ref.type === "Part") {
            item.style.display = "block";
        }
        else if (item.ref && item.ref.type === "Assembly") {
            showAllPartsInAccordion(item);
        }
    }
}
