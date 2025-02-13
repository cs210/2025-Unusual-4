'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { supabase } from '@/utils/supabase'
import type { Chat } from '@/utils/supabase'

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
        setChats(data)
      }
    }

    loadChatHistory()
  }, [])

  const handleChatClick = (chat: Chat) => {
    router.push(`/chat?id=${chat.id}`)
  }

  if (chats.length === 0) return null

  return (
    <div className="mb-8">
      <h2 className="text-xl font-semibold mb-4">Recent Chats</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {chats.map((chat) => (
          <button
            key={chat.id}
            onClick={() => handleChatClick(chat)}
            className="p-4 rounded-lg border border-gray-200 dark:border-gray-700 hover:border-blue-500 dark:hover:border-blue-400 transition-colors duration-200 text-left bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
            tabIndex={0}
            aria-label={`Continue chat: ${chat.title}`}
          >
            <h3 className="font-medium truncate">{chat.title}</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {new Date(chat.created_at).toLocaleDateString()}
            </p>
          </button>
        ))}
      </div>
    </div>
  )
}

export default ChatHistory 