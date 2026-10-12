function updateAssembly(intersect) {
  const selection_mode = document.querySelector('input[name="mode"]:checked');

  let newly_highlighted_surface = intersect.object.userData.surface;
  let newly_highlighted_shell = intersect.object.userData.shell;
  let newly_highlighted_model = intersect.object.userData.model;

  if (last_highlighted_model) {
    last_highlighted_model.shells.forEach((shell) => {
      shell.surfaces.forEach((surface) => {
        surface.mesh.material.reflectivity = 0;
        // if (selection_mode.value == "assembly")
        //     surface.mesh.material = surface.model.mat
        // else if (selection_mode.value == "shell")
        //     surface.mesh.material = surface.shell.mat
        // else
        surface.mesh.material = surface.mat;
      });
    });
  }

  if (hover_box) {
    if (renderer.mode == 0) renderer.scene.remove(hover_box);
    else if (renderer.mode == 1) renderer.scene_isolated.remove(hover_box);
    hover_box = null;
  }
  if (selection_mode.value == "assembly") {
    const boundingBox = new THREE.Box3().setFromObject(
      newly_highlighted_model.mesh
    );
    hover_box = new THREE.Box3Helper(boundingBox, 0x000000);
    renderer.scene.add(hover_box);

    hover_box.parent_body = newly_highlighted_model;
  } else if (selection_mode.value == "shell") {
    const boundingBox = new THREE.Box3().setFromObject(
      newly_highlighted_shell.mesh
    );
    hover_box = new THREE.Box3Helper(boundingBox, 0x000000);
    renderer.scene.add(hover_box);

    hover_box.parent_body = newly_highlighted_shell;
  } else if (selection_mode.value == "surface") {
    var edges = new THREE.EdgesGeometry(
      newly_highlighted_surface.mesh.geometry
    );
    var edgesMaterial = new THREE.LineBasicMaterial({ color: 0x000000 });
    hover_box = new THREE.LineSegments(edges, edgesMaterial);
    if (renderer.mode == 0) renderer.scene.add(hover_box);
    else if (renderer.mode == 1) renderer.scene_isolated.add(hover_box);
    hover_box.parent_body = newly_highlighted_surface;
  }
  hover_box.point = new THREE.Vector3().copy(intersect.point);

  last_highlighted_model = newly_highlighted_model;
  last_highlighted_shell = newly_highlighted_shell;
  last_highlighted_surface = newly_highlighted_surface;
}

function updateStl(intersect) {
  // TODO: highlight and selection of stl files
}

function animate() {
  requestAnimationFrame(animate);
  renderer.orbit_controls.update();

  renderer.raycaster.setFromCamera(renderer.mouse, renderer.camera);

  // Getting the point/mesh where the cursor is on screen
  var intersects;
  if (renderer.mode == 0)
    intersects = renderer.raycaster.intersectObjects(
      renderer.main_group.children
    );
  else if (renderer.mode == 1)
    intersects = renderer.raycaster.intersectObjects(
      renderer.isolated_group.children
    );

  if (intersects.length > 0) {
    const intersect = intersects[0];
    last_highlighted_mesh = intersect.object;

    switch (last_highlighted_mesh.userData.parent_body.type) {
      case "Stl":
        updateStl(intersect);
        break;
      case "Surface":
      case "Part":
      case "Assembly":
      // updateAssembly(intersect);
      case "3mf":
      // TODO
      default:
        break;
    }
  } else {
    if (hover_box) {
      if (renderer.mode == 0) renderer.scene.remove(hover_box);
      else if (renderer.mode == 1) renderer.scene_isolated.remove(hover_box);
      hover_box = null;
    }

    // if (last_highlighted_model) {
    //     last_highlighted_model.shells.forEach(shell => {
    //         shell.surfaces.forEach(surface => {
    //             surface.mesh.material.reflectivity = 0;
    //             if (selection_mode.value == "assembly")
    //                 surface.mesh.material.transmission = surface.model.transmission
    //             else if (selection_mode.value == "shell")
    //                 surface.mesh.material.transmission = surface.shell.transmission
    //             else
    //                 surface.mesh.material.transmission = surface.transmission
    //         });
    //     });
    // }

    last_highlighted_model = null;
    last_highlighted_shell = null;
    last_highlighted_surface = null;
    last_highlighted_mesh = null;
  }

  // if (selection_box.length == 1) {
  //     selection_box.forEach(box => {
  //         box.parent_body.mat.transmission = opacity.value / 100
  //         let col = hexToRgb(color.value)
  //         box.parent_body.mat.color.r = col.r
  //         box.parent_body.mat.color.g = col.g
  //         box.parent_body.mat.color.b = col.b
  //     })
  // }
  if (ContextMenu.ref != undefined) {
    ContextMenu.ref.mat.transmission = opacity.value / 100;
    let col = hexToRgb(color.value);
    ContextMenu.ref.mat.color.r = col.r;
    ContextMenu.ref.mat.color.g = col.g;
    ContextMenu.ref.mat.color.b = col.b;
    if (ContextMenu.ref.type == "Part") {
      ContextMenu.ref.surfaces.forEach((surf) => {
        surf.mesh.material = ContextMenu.ref.mat;
      });
    }
  }
  renderer.render();
}

function rgbToHex(col) {
  let r = col.r * 255,
    g = col.g * 255,
    b = col.b * 255;
  r = Math.max(0, Math.min(255, r));
  g = Math.max(0, Math.min(255, g));
  b = Math.max(0, Math.min(255, b));
  return (
    "#" +
    ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1).split(".")[0]
  );
}

function hexToRgb(hex) {
  var bigint = parseInt(hex.substring(1), 16);
  var r = (bigint >> 16) & 255;
  var g = (bigint >> 8) & 255;
  var b = bigint & 255;
  return { r: r / 255.0, g: g / 255.0, b: b / 255.0 };
}
