'use client'

import { useState, useEffect, useRef } from 'react'
import { useSearchParams, useRouter } from 'next/navigation'
import { Message } from '@/types/chat'
import { marked } from 'marked'
import { configureMarked } from '@/utils/markdown'
import { supabase } from '@/utils/supabase'
import type { Chat, ChatMessage } from '@/utils/supabase'

const ChatPage = () => {
  const router = useRouter()
  const searchParams = useSearchParams()
  const initialQuestion = searchParams.get('q')
  const templateId = searchParams.get('template')
  const chatId = searchParams.get('id')
  
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [currentChatId, setCurrentChatId] = useState<string | null>(chatId)
  const [chatTitle, setChatTitle] = useState<string>('')
  const [initialQuestionProcessed, setInitialQuestionProcessed] = useState(false)
  const [streamingMessage, setStreamingMessage] = useState('')
  const [isInitialized, setIsInitialized] = useState(false)
  
  // Use a ref to track ongoing requests
  const activeRequestRef = useRef<boolean>(false)

  // Configure marked when component mounts
  useEffect(() => {
    configureMarked()
  }, [])

  // Combined chat loading and initial question handling
  useEffect(() => {
    const initializeChat = async () => {
      if (!chatId || isInitialized) return
      setIsInitialized(true)
      
      try {
        // Load chat details
        const { data: chat } = await supabase
          .from('chats')
          .select('*')
          .eq('id', chatId)
          .single()

        if (chat) {
          setCurrentChatId(chat.id)
          setChatTitle(chat.title)
          
          // Add to localStorage
          const chatHistory = JSON.parse(localStorage.getItem('chatHistory') || '[]')
          if (!chatHistory.includes(chat.id)) {
            localStorage.setItem('chatHistory', JSON.stringify([...chatHistory, chat.id]))
          }

          // Load existing messages
          const { data: chatMessages } = await supabase
            .from('messages')
            .select('*')
            .eq('chat_id', chat.id)
            .order('created_at', { ascending: true })

          if (chatMessages) {
            setMessages(chatMessages.map(msg => ({
              role: msg.role as 'user' | 'assistant',
              content: msg.content
            })))

            // Handle initial question if it exists and no messages yet
            if (initialQuestion && chatMessages.length === 0 && !initialQuestionProcessed) {
              setInitialQuestionProcessed(true)
              // Add initial question to messages first
              const initialMessage: Message = { role: 'user', content: initialQuestion }
              setMessages([initialMessage])
              // Then send it to get the response
              handleSendMessage(initialQuestion)
            }
          }
        }
      } catch (error) {
        console.error('Error initializing chat:', error)
      }
    }

    initializeChat()
  }, [chatId, initialQuestion])

  // Reset initialization when chatId changes
  useEffect(() => {
    if (!chatId) {
      setIsInitialized(false)
      setInitialQuestionProcessed(false)
    }
  }, [chatId])

  const handleSendMessage = async (content: string) => {
    if (!content.trim() || activeRequestRef.current) return

    try {
      activeRequestRef.current = true
      const newMessage: Message = { role: 'user', content }
      setMessages(prev => [...prev, newMessage])
      setInput('')
      setIsLoading(true)
      setStreamingMessage('')

      if (currentChatId) {
        await supabase
          .from('messages')
          .insert([{
            chat_id: currentChatId,
            role: 'user',
            content
          }])
      }

      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          message: content, 
          messages: [...messages, newMessage]
        })
      })

      if (!response.ok || !response.body) {
        throw new Error('Stream response not available')
      }

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let fullMessage = ''

      try {
        while (true) {
          const { done, value } = await reader.read()
          if (done) break

          const text = decoder.decode(value)
          fullMessage += text
          setStreamingMessage(fullMessage)
        }

        if (currentChatId) {
          await supabase
            .from('messages')
            .insert([{
              chat_id: currentChatId,
              role: 'assistant',
              content: fullMessage
            }])
        }

        setMessages(prev => [...prev, { role: 'assistant', content: fullMessage }])
      } finally {
        setStreamingMessage('')
      }
    } catch (error) {
      console.error('Error:', error)
    } finally {
      setIsLoading(false)
      activeRequestRef.current = false
    }
  }

  const renderMarkdown = (content: string) => {
    return { __html: marked(content) }
  }

  return (
    <div className="min-h-screen p-4 max-w-3xl mx-auto bg-gray-50 dark:bg-gray-900">
      {chatTitle && (
        <h1 className="text-xl font-semibold mb-6 text-gray-900 dark:text-white">
          {chatTitle}
        </h1>
      )}
      <div className="space-y-4 mb-24">
        {messages.map((message, index) => (
          <div
            key={index}
            className={`p-4 rounded-lg ${
              message.role === 'user' 
                ? 'bg-blue-100 dark:bg-blue-900 text-gray-900 dark:text-white ml-12' 
                : 'bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-white mr-12'
            }`}
          >
            <div 
              className="prose dark:prose-invert max-w-none"
              dangerouslySetInnerHTML={renderMarkdown(message.content)}
            />
          </div>
        ))}
        
        {streamingMessage && (
          <div className="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg mr-12 text-gray-900 dark:text-white">
            <div 
              className="prose dark:prose-invert max-w-none"
              dangerouslySetInnerHTML={renderMarkdown(streamingMessage)}
            />
          </div>
        )}
        
        {isLoading && !streamingMessage && (
          <div className="bg-gray-100 dark:bg-gray-800 p-4 rounded-lg mr-12 animate-pulse text-gray-900 dark:text-white">
            Thinking...
          </div>
        )}
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault()
          handleSendMessage(input)
        }}
        className="fixed bottom-0 left-0 right-0 p-4 bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-700"
      >
        <div className="max-w-3xl mx-auto flex gap-2">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            className="flex-1 p-2 border rounded-lg focus:ring-2 focus:ring-blue-500 outline-none bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
            placeholder="Type your message..."
            aria-label="Chat message"
          />
          <button
            type="submit"
            className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:hover:bg-blue-500"
            disabled={isLoading || !input.trim()}
            aria-label="Send message"
          >
            Send
          </button>
        </div>
      </form>
    </div>
  )
}

export default ChatPage 