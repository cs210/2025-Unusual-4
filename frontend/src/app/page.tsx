"use client"

import { motion } from "framer-motion"
import { FloatingARObjects } from "@/components/FloatingARObjects"
import { ARHeadsetAnimation } from "@/components/ARHeadsetAnimation"
import QuestionInput from "@/components/QuestionInput"
import ChatHistory from "@/components/ChatHistory"
import ChatTemplates from "@/components/ChatTemplates"
import Navbar from "@/components/Navbar"
import { SparklesCore } from "@/components/SparklesCore"

const containerVariants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
    },
  },
}

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
}

export default function Home() {
  return (
    <main className="min-h-screen bg-black/[0.96] antialiased bg-grid-white/[0.02] relative overflow-hidden">
      {/* Navbar */}
      <Navbar />

      {/* Glittering stars background */}
      <div className="absolute inset-0 z-0">
        <SparklesCore
          background="transparent"
          minSize={0.4}
          maxSize={1}
          particleDensity={100}
          className="w-full h-full"
          particleColor="#FFFFFF"
        />
      </div>

      {/* Floating AR objects background */}
      <div className="absolute inset-0 overflow-hidden z-10">
        <FloatingARObjects count={6} />
      </div>

      <div className="container mx-auto px-6 relative z-20 flex flex-col justify-center min-h-screen">
        <motion.div
          className="max-w-4xl mx-auto space-y-8"
          variants={containerVariants}
          initial="hidden"
          animate="show"
        >
          <div className="flex flex-col items-center justify-center">
            <motion.h1
              className="text-5xl md:text-6xl lg:text-7xl font-bold text-center text-white mb-8"
              variants={itemVariants}
            >
              Accelerate XR Development with{" "}
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-pink-600">XeleR</span>
            </motion.h1>

            <motion.p variants={itemVariants} className="text-gray-400 text-xl mb-8 max-w-2xl mx-auto text-center">
              Transform your XR projects with our AI-powered development tools. Streamline workflows, generate immersive
              content, and bring your ideas to life faster than ever.
            </motion.p>

            <motion.div variants={itemVariants} className="w-full max-w-2xl mb-12">
              <QuestionInput />
            </motion.div>
          </div>

          <motion.div variants={itemVariants}>
            <ChatHistory />
          </motion.div>

          <motion.div variants={itemVariants}>
            <ChatTemplates />
          </motion.div>
        </motion.div>
      </div>

      {/* Animated AR headset */}
      <div className="absolute bottom-4 right-4 w-80 h-80 z-30">
        <ARHeadsetAnimation />
      </div>
    </main>
  )
}

