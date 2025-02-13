'use client'

import QuestionInput from '@/components/QuestionInput'
import ChatTemplates from '@/components/ChatTemplates'
import ChatHistory from '@/components/ChatHistory'
import { motion } from 'framer-motion'

const containerVariants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1
    }
  }
}

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
}

export default function Home() {
  return (
    <main className="min-h-screen p-8 bg-slate-900">
      <motion.div 
        className="space-y-8 max-w-6xl mx-auto"
        variants={containerVariants}
        initial="hidden"
        animate="show"
      >
        <motion.h1 
          className="text-4xl font-bold text-center text-white"
          variants={itemVariants}
        >
          AI Chat Assistant
        </motion.h1>
        
        <motion.div variants={itemVariants}>
          <QuestionInput />
        </motion.div>
        
        <motion.div 
          className="bg-slate-800/30 rounded-2xl p-6"
          variants={itemVariants}
        >
          <ChatHistory />
        </motion.div>
        
        <motion.div 
          className="bg-slate-800/30 rounded-2xl p-6"
          variants={itemVariants}
        >
          <h2 className="text-2xl font-semibold mb-6 text-white">Chat Templates</h2>
          <ChatTemplates />
        </motion.div>
      </motion.div>
    </main>
  )
} 