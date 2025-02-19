"use client"
import { motion } from "framer-motion"
import type React from "react"
import dynamic from "next/dynamic"

const ScenePreview = dynamic(() => import("@/components/ScenePreview"), { ssr: false })

interface ChatTemplatesProps {
  onSelectTemplate: (template: string, code?: string) => void
}

const ChatTemplates: React.FC<ChatTemplatesProps> = ({ onSelectTemplate }) => {
  const templates = [
    {
      question: "Render a 3D cube",
      code: `function initScene() {
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
          animate();`,
    },
    {
      question: "Make the cube jump",
      code: `
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
                // Jump animation for the cube
                new TWEEN.Tween(cube.position)
                    .to({ y: 5 }, 500) // Jump up to y = 5
                    .easing(TWEEN.Easing.Quadratic.Out)
                    .onComplete(() => {
                        // Go back down to initial position after jumping
                        new TWEEN.Tween(cube.position)
                            .to({ y: 0 }, 500) // Go back down to initial position
                            .easing(TWEEN.Easing.Quadratic.In)
                            .start();
                    })
                    .start();
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
      `,
    },
  ]

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      className="bg-gradient-to-br from-purple-900/30 to-pink-900/30 rounded-2xl p-6 backdrop-blur-sm border border-purple-500/20"
    >
      <h2 className="text-2xl font-semibold mb-6 text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-pink-600">
        Chat Templates
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {templates.map((template, index) => (
          <motion.button
            key={index}
            className="bg-gradient-to-r from-purple-800/50 to-pink-800/50 hover:from-purple-700/50 hover:to-pink-700/50 text-white rounded-lg p-4 transition-colors backdrop-blur-sm border border-purple-500/20 flex flex-col items-start text-left w-full"
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            onClick={() => onSelectTemplate(template.question, template.code)}
          >
            <h3 className="font-medium text-lg mb-4">{template.question}</h3>
            {template.code && (
              <div className="mt-auto w-full">
                <ScenePreview code={template.code} />
              </div>
            )}
          </motion.button>
        ))}
      </div>
    </motion.div>
  )
}

export default ChatTemplates

