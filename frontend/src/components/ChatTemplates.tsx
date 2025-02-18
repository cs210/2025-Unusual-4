"use client"
import { motion } from "framer-motion"

const ChatTemplates = () => {
  const templates = [
    "How do I set up an AR scene in Unity?",
    "What are the best practices for AR user interface design?",
    "Can you explain the difference between ARKit and ARCore?",
    "How can I optimize AR performance on mobile devices?",
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
            className="bg-gradient-to-r from-purple-800/50 to-pink-800/50 hover:from-purple-700/50 hover:to-pink-700/50 text-white rounded-lg p-4 text-left transition-colors backdrop-blur-sm border border-purple-500/20"
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
          >
            {template}
          </motion.button>
        ))}
      </div>
    </motion.div>
  )
}

export default ChatTemplates

