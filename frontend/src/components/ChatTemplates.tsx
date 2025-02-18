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
    //{ question: "How do I set up an AR scene in Unity?", code: null },
    //{ question: "What are the best practices for AR user interface design?", code: null },
    //{ question: "Can you explain the difference between ARKit and ARCore?", code: null },
    //{ question: "How can I optimize AR performance on mobile devices?", code: null },
    {
      question: "Render a 3D cube",
      code: `
// Create a rotating cube
const geometry = new THREE.BoxGeometry(1, 1, 1);
const material = new THREE.MeshPhongMaterial({ color: 0x00ff00 });
const cube = new THREE.Mesh(geometry, material);
scene.add(cube);

// Add animation to the cube
function animate() {
  requestAnimationFrame(animate);
  cube.rotation.x += 0.01;
  cube.rotation.y += 0.01;
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

