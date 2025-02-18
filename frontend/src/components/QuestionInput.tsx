"use client"

import type React from "react"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { supabase } from "@/utils/supabase"
import { motion } from "framer-motion"
import { Send } from "lucide-react"

const QuestionInput = () => {
  const router = useRouter()
  const [question, setQuestion] = useState("")
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!question.trim() || isSubmitting) return

    setIsSubmitting(true)

    try {
      const { data: chat, error } = await supabase
        .from("chats")
        .insert([
          {
            title: question.substring(0, 50),
            is_template: false,
          },
        ])
        .select()
        .single()

      if (error) throw error

      if (chat) {
        const chatHistory = JSON.parse(localStorage.getItem("chatHistory") || "[]")
        localStorage.setItem("chatHistory", JSON.stringify([chat.id, ...chatHistory]))

        router.push(`/chat?id=${chat.id}&q=${encodeURIComponent(question)}`)
      }
    } catch (error) {
      console.error("Error creating chat:", error)
      // You might want to show an error message to the user here
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <motion.form
      onSubmit={handleSubmit}
      className="w-full max-w-3xl mx-auto"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <div className="relative">
        <input
          type="text"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="Ask anything about XR development..."
          className="w-full py-4 px-6 bg-gray-900/50 text-white placeholder-gray-400 rounded-full border border-gray-700 focus:outline-none focus:border-purple-500 transition-colors duration-200"
          aria-label="Ask a question"
          disabled={isSubmitting}
        />
        <motion.button
          type="submit"
          className="absolute right-2 top-1/2 transform -translate-y-1/2 bg-purple-600 hover:bg-purple-700 text-white rounded-full p-2 transition-colors duration-200 disabled:opacity-50 disabled:hover:bg-purple-600"
          aria-label="Submit question"
          disabled={isSubmitting}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
        >
          <Send size={20} />
        </motion.button>
      </div>
    </motion.form>
  )
}

export default QuestionInput

