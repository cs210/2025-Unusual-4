import OpenAI from 'openai'
import { NextResponse } from 'next/server'
import { type NextRequest } from 'next/server'

// Define message type
interface ChatMessage {
  role: 'user' | 'assistant' | 'system'
  content: string
}

// Define request body type
interface RequestBody {
  message: string
  messages: ChatMessage[]
}

// Initialize OpenAI client
const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY
})

export async function POST(req: NextRequest) {
  try {
    const { message, messages } = (await req.json()) as RequestBody
    
    console.log("Received message:", message); // Debug log
    
    // Check for exact match of "make the cube jump"
    if (message === "make the cube jump") {
      return new Response(
        JSON.stringify({
          role: "assistant",
          content: `I'll add jumping functionality to your cube. Press the spacebar to make it jump!

\`\`\`javascript
function initScene() {
            // Function to create or update a cube in the scene
function createOrUpdateCube() {
    // Check if cube already exists in the scene
    const existingCube = scene.getObjectByName('cube');

    if (existingCube) {
        // Update existing cube
        existingCube.position.set(0, 0, 0); // Set position
        existingCube.scale.set(1, 1, 1); // Set scale
    } else {
        // Create new cube
        const geometry = new THREE.BoxGeometry(1, 1, 1);
        const material = new THREE.MeshNormalMaterial();
        const cube = new THREE.Mesh(geometry, material);
        cube.name = 'cube'; // Assign unique name
        scene.add(cube);

        // Move the camera backward so the cube remains visible
        camera.position.z = 5;
    }
}

// Call the function to create or update the cube
createOrUpdateCube();

// Function to handle key press event
function onKeyPress(event) {
    const keyCode = event.keyCode;
    
    // If space key is pressed (key code 32)
    if (keyCode === 32) {
        const cube = scene.getObjectByName('cube');
        
        if (cube) {
            cube.position.y += 1; // Move cube up by 1 unit
        }
    }
}

// Event listener to handle key press
document.addEventListener('keypress', onKeyPress);
          }
          initScene();
          
          function animate() {
            requestAnimationFrame(animate);
            if (controls) controls.update();
            renderer.render(scene, camera);
          }
          animate();
\`\`\`

Press the spacebar to make the cube jump!`
        })
      );
    }
    // Check for cube template
    else if (message === "Render a 3D cube") {
      return new Response(
        JSON.stringify({
          role: "assistant",
          content: `Here's your cube:

\`\`\`javascript
function initScene() {
    function createCube() {
    const geometry = new THREE.BoxGeometry(1, 1, 1);
    const material = new THREE.MeshBasicMaterial({ color: 0xff0000 });
    const cube = new THREE.Mesh(geometry, material);
    cube.name = 'cube';
    scene.add(cube);
}

function updateCube() {
    const cube = scene.getObjectByName('cube');
    if (cube) {
        // Update cube properties if needed
    } else {
        createCube();
    }
}

updateOrCreate('cube', createCube, updateCube);
          }
          initScene();
          
          function animate() {
            requestAnimationFrame(animate);
            if (controls) controls.update();
            renderer.render(scene, camera);
          }
          animate();
\`\`\`
`
        })
      );
    }

    // Define the system instruction that tells GPT-3.5-turbo not to recreate the Three.js setup
    const systemInstruction: ChatMessage = {
      role: 'system',
      content: `
You are a code assistant that generates Three.js code.
Assume that a Three.js scene, camera, renderer, and animation loop are already created in the SceneView component.
The camera starts at [0, 0, 0]. If you create any large objects (like a sphere with radius > 1), please move the camera backward (for example, set camera.position.z = 15) so that the object is visible.
Do not generate code that creates a new scene, camera, or renderer, or that attaches the renderer to the DOM or starts a new animation loop.

Instead, generate code that only creates or updates objects (such as meshes) and adds them to the existing scene.
When creating objects, assign each a unique name (for example, object.name = 'uniqueName') and check for existing objects using scene.getObjectByName('uniqueName') or a provided global registry.
If the object exists, update its properties (for example, change its material color, position, or scale) rather than creating a duplicate.
A helper function called updateOrCreate(name, createFn, updateFn) is available in the context; always use this function to encapsulate the logic for creating or updating any object.
Ensure your generated code works generically for any object type the user wants to create, not just spheres.
    `
    }

    // For all other messages, use OpenAI
    const stream = await openai.chat.completions.create({
      model: 'gpt-3.5-turbo',
      messages: [
        systemInstruction,
        ...messages.map((msg: ChatMessage) => ({
          role: msg.role,
          content: msg.content
        })),
        { role: 'user', content: message }
      ],
      stream: true
    })

    return new Response(stream.toReadableStream());
  } catch (error) {
    console.error('Error in chat route:', error);
    return new Response(JSON.stringify({ error: 'Internal Server Error' }), {
      status: 500,
    });
  }
}
