// Find the UR3 game object
GameObject ur3 = GameObject.Find("UR3");

// Find the Directional Light game object
GameObject directionalLight = GameObject.Find("Directional Light");

// Make the Directional Light a child of the UR3 game object
directionalLight.transform.parent = ur3.transform;
