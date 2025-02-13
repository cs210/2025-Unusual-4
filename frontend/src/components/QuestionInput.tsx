'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { supabase } from '@/utils/supabase'

const QuestionInput = () => {
  const router = useRouter()
  const [question, setQuestion] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!question.trim() || isSubmitting) return
    
    setIsSubmitting(true)

    try {
      // Create chat first
      const { data: chat } = await supabase
        .from('chats')
        .insert([{ 
          title: question.substring(0, 50),
          is_template: false
        }])
        .select()
        .single()

      if (chat) {
        // Add to localStorage
        const chatHistory = JSON.parse(localStorage.getItem('chatHistory') || '[]')
        localStorage.setItem('chatHistory', JSON.stringify([...chatHistory, chat.id]))
        
        // Navigate to chat with ID
        router.push(`/chat?id=${chat.id}&q=${encodeURIComponent(question)}`)
      }
    } catch (error) {
      console.error('Error creating chat:', error)
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="w-full">
      <div className="relative">
        <input
          type="text"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="Ask me anything..."
          className="w-full p-4 pr-12 rounded-lg border border-gray-300 dark:border-gray-700 focus:border-blue-500 focus:ring-2 focus:ring-blue-500 outline-none bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
          aria-label="Ask a question"
          disabled={isSubmitting}
        />
        <button
          type="submit"
          className="absolute right-2 top-1/2 -translate-y-1/2 p-2 text-blue-500 hover:text-blue-600 dark:text-blue-400 dark:hover:text-blue-300 disabled:opacity-50"
          aria-label="Submit question"
          disabled={isSubmitting}
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
        </button>
      </div>
    </form>
  )
}

export default QuestionInput 