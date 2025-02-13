'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { supabase } from '@/utils/supabase'
import type { Chat } from '@/utils/supabase'
import { motion, AnimatePresence } from 'framer-motion'

const ChatTemplates = () => {
  const router = useRouter()
  const [templates, setTemplates] = useState<Chat[]>([])
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    const loadTemplates = async () => {
      const { data } = await supabase
        .from('chats')
        .select('*')
        .eq('is_template', true)
        .order('template_category', { ascending: true })

      if (data) {
        setTemplates(data)
      }
    }

    loadTemplates()
  }, [])

  const handleTemplateClick = async (template: Chat) => {
    if (isLoading) return
    setIsLoading(true)

    try {
      // First, create a new chat based on the template
      const { data: newChat } = await supabase
        .from('chats')
        .insert([{
          title: template.title,
          is_template: false,
          description: template.description,
          template_category: template.template_category,
          template_image: template.template_image
        }])
        .select()
        .single()

      if (newChat) {
        // Get template messages
        const { data: templateMessages } = await supabase
          .from('messages')
          .select('*')
          .eq('chat_id', template.id)
          .order('created_at', { ascending: true })

        // Copy messages to new chat if they exist
        if (templateMessages && templateMessages.length > 0) {
          await supabase
            .from('messages')
            .insert(
              templateMessages.map(msg => ({
                chat_id: newChat.id,
                role: msg.role,
                content: msg.content
              }))
            )
        }

        // Add to localStorage
        const chatHistory = JSON.parse(localStorage.getItem('chatHistory') || '[]')
        localStorage.setItem('chatHistory', JSON.stringify([...chatHistory, newChat.id]))

        // Navigate to the new chat
        router.push(`/chat?id=${newChat.id}`)
      }
    } catch (error) {
      console.error('Error creating chat from template:', error)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <AnimatePresence>
        {templates.map((template) => (
          <motion.button
            key={template.id}
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.95 }}
            whileHover={{ scale: 1.02 }}
            whileTap={{ scale: 0.98 }}
            onClick={() => handleTemplateClick(template)}
            className="p-6 rounded-xl border border-slate-700 hover:border-blue-500 transition-colors duration-200 text-left bg-slate-800/50 text-white group disabled:opacity-50"
            tabIndex={0}
            aria-label={`Start ${template.title} chat`}
            disabled={isLoading}
          >
            <div className="flex items-center gap-3 mb-4">
              <span className="text-2xl group-hover:scale-110 transition-transform">
                {template.template_image}
              </span>
              <div>
                <h3 className="font-medium group-hover:text-blue-400 transition-colors">
                  {template.title}
                </h3>
                <p className="text-sm text-slate-400">
                  {template.template_category}
                </p>
              </div>
            </div>
            <p className="text-sm text-slate-300">
              {template.description}
            </p>
          </motion.button>
        ))}
      </AnimatePresence>
    </div>
  )
}

export default ChatTemplates 