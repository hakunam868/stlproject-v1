
function centerModelAboveGround(model, groundHeight = 0) {
    const boundingBox = new THREE.Box3().setFromObject(model);
    const boundingBoxCenter = new THREE.Vector3();
    boundingBox.getCenter(boundingBoxCenter);
    const heightAboveGround = boundingBox.max.z - boundingBoxCenter.z;
    const modelPosition = new THREE.Vector3(0, 0, groundHeight + heightAboveGround).sub(boundingBoxCenter);
    model.position.copy(modelPosition);
}

function getRandomFloat() {
    const randomBuffer = new Uint32Array(1);
    window.crypto.getRandomValues(randomBuffer);
    return randomBuffer[0] / (0xffffffff + 1);
}