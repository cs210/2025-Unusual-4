'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { supabase } from '@/utils/supabase'
import { motion } from 'framer-motion'

const QuestionInput = () => {
  const router = useRouter()
  const [question, setQuestion] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!question.trim() || isSubmitting) return
    
    setIsSubmitting(true)

    try {
      const { data: chat } = await supabase
        .from('chats')
        .insert([{ 
          title: question.substring(0, 50),
          is_template: false
        }])
        .select()
        .single()

      if (chat) {
        const chatHistory = JSON.parse(localStorage.getItem('chatHistory') || '[]')
        localStorage.setItem('chatHistory', JSON.stringify([...chatHistory, chat.id]))
        
        router.push(`/chat?id=${chat.id}&q=${encodeURIComponent(question)}`)
      }
    } catch (error) {
      console.error('Error creating chat:', error)
      setIsSubmitting(false)
    }
  }

  return (
    <motion.form 
      onSubmit={handleSubmit} 
      className="w-full"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
    >
      <div className="relative max-w-2xl mx-auto">
        <input
          type="text"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="Ask me anything..."
          className="w-full p-4 pr-12 rounded-xl border border-slate-700 focus:border-blue-500 focus:ring-2 focus:ring-blue-500 outline-none bg-slate-800/50 text-white placeholder-slate-400"
          aria-label="Ask a question"
          disabled={isSubmitting}
        />
        <motion.button
          type="submit"
          className="absolute right-2 top-1/2 -translate-y-1/2 p-2 text-slate-400 hover:text-blue-400 disabled:opacity-50 disabled:hover:text-slate-400"
          aria-label="Submit question"
          disabled={isSubmitting}
          whileHover={{ scale: 1.1 }}
          whileTap={{ scale: 0.95 }}
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            strokeWidth={1.5}
            stroke="currentColor"
            className="w-6 h-6"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5"
            />
          </svg>
        </motion.button>
      </div>
    </motion.form>
  )
}

export default QuestionInput 