class Renderer {
    constructor(container) {
        this.container = container;
        this.scene = new THREE.Scene();
        this.overlay_scene = new THREE.Scene();
        this.scene_isolated = new THREE.Scene();
        this.sceneOrtho = new THREE.Scene();

        this.camera = new THREE.PerspectiveCamera(35, 1, 0.1, 10000);
        this.camera.up.set(0, 0, 1);

        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.renderer.autoClear = false;

        this.cameraOrtho = new THREE.OrthographicCamera();
        this.cameraOrtho.position.z = 10;

        this.canvas = this.renderer.domElement;
        this.orbit_controls = new THREE.OrbitControls(this.camera, this.canvas);
        this.transform_controls = new THREE.TransformControls(
            this.camera,
            this.canvas
        );
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();

        this.main_group = new THREE.Group();
        this.isolated_group = new THREE.Group();

        this.mode = 0;

        this.init();
    }

    init() {
        this.container.appendChild(this.canvas);

        this.camera.position.set(0, 0, 10);
        this.renderer.setClearColor(0x000000, 0);

        this.orbit_controls.mouseButtons = {
            MIDDLE: THREE.MOUSE.PAN,
            RIGHT: THREE.MOUSE.ROTATE,
        };

        // Adding many light in 3D
        for (var x = -999; x <= 999; x += 1998) {
            for (var y = -999; y <= 999; y += 1998) {
                for (var z = -999; z <= 999; z += 1998) {
                    var light = new THREE.DirectionalLight(0xffffff, 0.4);
                    light.position.set(x, y, z);
                    this.scene.add(light);

                    var light = new THREE.DirectionalLight(0xffffff, 0.4);
                    light.position.set(x, y, z);
                    this.scene_isolated.add(light);

                    var light = new THREE.DirectionalLight(0xffffff, 0.4);
                    light.position.set(x, y, z);
                    this.overlay_scene.add(light);
                }
            }
        }

        this.scene.add(this.transform_controls);
        this.scene.add(this.main_group);
        this.scene_isolated.add(this.isolated_group);
    }

    render() {
        this.renderer.clear();
        switch (this.mode) {
            case 0:
                this.renderer.render(this.scene, this.camera);
                break;

            case 1:
                this.renderer.render(this.scene_isolated, this.camera);
                break;

            default:
                break;
        }

        this.renderer.clearDepth();
        this.renderer.render(this.sceneOrtho, this.cameraOrtho);
        this.renderer.clearDepth();
        this.renderer.render(this.overlay_scene, this.camera);
    }

    resizeCanvas() {
        const { width, height } = container.getBoundingClientRect();
        this.renderer.setSize(width, height);
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();

        this.cameraOrtho.left = -width / 2;
        this.cameraOrtho.right = width / 2;
        this.cameraOrtho.top = height / 2;
        this.cameraOrtho.bottom = -height / 2;
        this.cameraOrtho.updateProjectionMatrix();
    }
}

// Base class that hold any 3d model which is there
class PlotBase {
    constructor(mesh) {
        // to hold geometrical data reference from THREE js
        this.mesh = mesh;

        // visual material of that part/surface/assembly
        this.mat = new THREE.MeshPhysicalMaterial({
            color: 0xffffff,
            metalness: 0,
            roughness: 0.8,
            transmission: 0.0,
            clearcoat: 0.1,
            clearcoatRoughness: 0.1,
            reflectivity: 0.1,
            side: THREE.DoubleSide,
        });

        // isSelected (TODO: impliment)
        this.selected = true;

        // transparency 0 is opaque and 1 is completely transparent
        this.transmission = 0;

        // comment (we can potentially replace this with discussoins)
        this.comment = "";

        this.type;

        this.mesh.userData.parent_body = this;
    }
}

// Surface option in UI
class SolidSurface extends PlotBase {
    constructor(shell, model, item, faces) {
        const geom = new THREE.BufferGeometry();
        geom.setAttribute(
            "position",
            new THREE.BufferAttribute(
                new Float32Array(item.attributes.position.array),
                3
            )
        );

        const fac = [];
        for (let j = faces.first; j <= faces.last; j++) {
            fac.push(item.index.array[j * 3]);
            fac.push(item.index.array[j * 3 + 1]);
            fac.push(item.index.array[j * 3 + 2]);
        }

        geom.setIndex(new THREE.BufferAttribute(new Uint32Array(fac), 1));

        geom.computeVertexNormals();

        let mesh = new THREE.Mesh(geom);
        super(mesh);

        this.shell = shell;
        this.model = model;

        this.mat.color.r = this.shell.mat.color.r;
        this.mat.color.g = this.shell.mat.color.g;
        this.mat.color.b = this.shell.mat.color.b;

        this.mesh.material = this.mat;

        this.mesh.userData.surface = this;
        this.mesh.userData.shell = shell;
        this.mesh.userData.model = model;

        this.type = "Surface";

        //this.accordion = createAccordion({
        //    title: "Surface"
        //})
        //this.accordion.style.zIndex = 3
    }
}

// Part option in UI
class SolidShell extends PlotBase {
    constructor(model, item, brep_faces) {
        let mesh = new THREE.Group();
        super(mesh);

        this.model = model;

        // List of surfaces (instances of surface class)
        this.surfaces = [];

        this.mesh.userData.shell = this;

        if (item.color) {
            this.mat.color.r = item.color[0];
            this.mat.color.g = item.color[1];
            this.mat.color.b = item.color[2];
        } else {
            this.mat.color.r = getRandomFloat() / 2.0 + 0.5;
            this.mat.color.g = getRandomFloat() / 2.0 + 0.5;
            this.mat.color.b = getRandomFloat() / 2.0 + 0.5;
        }

        this.accordion = createAccordion({
            title: item.name ? item.name : "Part",
            color: this.mat.color,
            ref: this,
            img: `${getBaseUrl()}images/part_3d.png`,
        });

        this.accordion.style.zIndex = 2;

        brep_faces.forEach((faces) => {
            // let faces = brep_faces[10]
            let new_surface = new SolidSurface(this, model, item, faces);
            this.surfaces.push(new_surface);
            this.mesh.add(new_surface.mesh);
            //this.accordion.appendChild(new_surface.accordion)
        });

        this.type = "Part";
    }
}

// Assembly option in UI
class SolidPart extends PlotBase {
    constructor(items, root) {
        let mesh = new THREE.Group();
        super(mesh);

        // List of parts
        this.shells = [];
        this.mesh.userData.model = this;
        // Example usage
        console.log("Assembly", root.children[0].name);
        this.accordion = createAccordion({
            title: root.children[0].name ? root.children[0].name : "Main Assembly",
            //title: "Laser Module",

            ref: this,
            img: `${getBaseUrl()}images/assembly_3d.png`,
        });
        this.accordion.style.zIndex = 1;

        items.forEach((item) => {
            let new_shell = new SolidShell(this, item, item.brep_faces);
            this.shells.push(new_shell);
            this.mesh.add(new_shell.mesh);
            this.accordion.appendChild(new_shell.accordion);
        });

        assemblies.appendChild(this.accordion);

        this.type = "Assembly";
    }
}

class STLPart extends PlotBase {
    constructor(byteData) {
        const loader = new THREE.STLLoader();
        const geometry = loader.parse(byteData);
        let mesh = new THREE.Mesh(geometry, default_material);
        super(mesh);

        this.type = "Stl";
    }
}

class Part3MF extends PlotBase {
    constructor(byteData) {
        const loader = new THREE.ThreeMFLoader();
        let mesh = loader.parse(byteData);
        super(mesh);

        function add_parent(node, ref) {
            if (node.type == "Group") {
                node.children.forEach((node) => add_parent(node, ref));
            } else {
                node.userData.parent_body = ref;
            }
        }

        add_parent(mesh, this);

        this.type = "3mf";
    }
}
class Draggable {
    constructor(grabElement, moveElement) {
        this.grabElement = grabElement;
        this.moveElement = moveElement;

        this.isDragging = false;
        this.offset = { x: 0, y: 0 };

        this.init();
    }

    init() {
        this.grabElement.style.cursor = "grab";

        this.grabElement.addEventListener(
            "mousedown",
            this.handleMouseDown.bind(this)
        );
        document.addEventListener("mousemove", this.handleMouseMove.bind(this));
        document.addEventListener("mouseup", this.handleMouseUp.bind(this));
    }

    handleMouseDown(event) {
        this.isDragging = true;
        this.offset = {
            x: event.clientX - selection_box[0].offx,
            y: event.clientY - selection_box[0].offy,
        };
        this.grabElement.style.cursor = "grabbing";
    }

    handleMouseMove(event) {
        if (!this.isDragging) return;

        selection_box[0].offx = event.clientX - this.offset.x;
        selection_box[0].offy = event.clientY - this.offset.y;
    }

    handleMouseUp() {
        this.isDragging = false;
        this.grabElement.style.cursor = "grab";
    }
}