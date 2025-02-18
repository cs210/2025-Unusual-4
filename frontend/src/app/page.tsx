"use client"

import { motion } from "framer-motion"
import { useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import { FloatingARObjects } from "@/components/FloatingARObjects"
import { ARHeadsetAnimation } from "@/components/ARHeadsetAnimation"
import QuestionInput from "@/components/QuestionInput"
import ChatTemplates from "@/components/ChatTemplates"
import Navbar from "@/components/Navbar"
import { SparklesCore } from "@/components/SparklesCore"

export default function Home() {
  const [isLoading, setIsLoading] = useState(false)
  const router = useRouter()

  const handleQuestionSubmit = useCallback(
    async (question: string, code?: string) => {
      setIsLoading(true)
      try {
        const queryParams = new URLSearchParams({ q: question })
        if (code) {
          queryParams.append("code", encodeURIComponent(code))
        }
        router.push(`/chat?${queryParams.toString()}`)
      } catch (error) {
        console.error("Error submitting question:", error)
        setIsLoading(false)
      }
    },
    [router],
  )

  return (
    <main className="min-h-screen bg-black/[0.96] antialiased bg-grid-white/[0.02] relative overflow-hidden">
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

      <div className="relative z-20 flex flex-col min-h-screen">
        <div className="flex-grow" />
        <div className="container mx-auto px-6 flex flex-col items-center justify-center">
          <motion.div
            className="max-w-4xl mx-auto space-y-12 text-center mb-20"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5 }}
          >
            <motion.h1
              className="text-5xl md:text-6xl lg:text-7xl font-bold text-white mb-6"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.2, duration: 0.5 }}
            >
              Accelerate XR Development with{" "}
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-pink-600">XeleR</span>
            </motion.h1>

            <motion.p
              className="text-gray-400 text-xl mb-8 max-w-2xl mx-auto"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.4, duration: 0.5 }}
            >
              Streamline workflows, generate immersive content, and bring your ideas to life faster than ever.
            </motion.p>

            <motion.div
              className="w-full max-w-2xl mx-auto"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.6, duration: 0.5 }}
            >
              <QuestionInput onSubmit={handleQuestionSubmit} isLoading={isLoading} />
            </motion.div>
          </motion.div>
        </div>

        <div className="container mx-auto px-6 pb-20">
          <motion.div
            className="max-w-4xl mx-auto"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.8, duration: 0.5 }}
          >
            <ChatTemplates onSelectTemplate={handleQuestionSubmit} />
          </motion.div>
        </div>
      </div>

      {/* Animated AR headset */}
      <div className="absolute bottom-4 right-4 w-80 h-80 z-30">
        <ARHeadsetAnimation />
      </div>
    </main>
  )
}

