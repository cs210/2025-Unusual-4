'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { supabase } from '@/utils/supabase'
import type { Chat } from '@/utils/supabase'
import { motion, AnimatePresence } from 'framer-motion'

const ChatHistory = () => {
  const router = useRouter()
  const [chats, setChats] = useState<Chat[]>([])

  useEffect(() => {
    const loadChatHistory = async () => {
      // Get chat IDs from localStorage
      const chatIds = JSON.parse(localStorage.getItem('chatHistory') || '[]')
      
      if (chatIds.length === 0) return

      // Fetch chat details from Supabase
      const { data } = await supabase
        .from('chats')
        .select('*')
        .in('id', chatIds)
        .order('created_at', { ascending: false })

      if (data) {
        // Keep only the latest 9 chats
        setChats(data.slice(0, 9))
      }
    }

    loadChatHistory()
  }, [])

  const handleChatClick = (chat: Chat) => {
    router.push(`/chat?id=${chat.id}`)
  }

  if (chats.length === 0) return null

  return (
    <div>
      <h2 className="text-2xl font-semibold mb-6 text-white">Recent Chats</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <AnimatePresence>
          {chats.map((chat) => (
            <motion.button
              key={chat.id}
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              onClick={() => handleChatClick(chat)}
              className="p-4 rounded-xl border border-slate-700 hover:border-blue-500 transition-colors duration-200 text-left bg-slate-800/50 text-white group"
              tabIndex={0}
              aria-label={`Continue chat: ${chat.title}`}
            >
              <h3 className="font-medium truncate group-hover:text-blue-400 transition-colors">
                {chat.title}
              </h3>
              <p className="text-sm text-slate-400">
                {new Date(chat.created_at).toLocaleDateString()}
              </p>
            </motion.button>
          ))}
        </AnimatePresence>
      </div>
    </div>
  )
}

export default ChatHistory 