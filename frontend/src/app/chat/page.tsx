'use client'

import { AnimatePresence, motion } from 'framer-motion'
import { Suspense, useEffect, useRef, useState, useCallback } from 'react'
import dynamic from 'next/dynamic'

// Dynamically import components that use browser APIs
const CodeView = dynamic(() => import('@/components/CodeView'), { ssr: false })
const SceneView = dynamic(() => import('@/components/SceneView'), { ssr: false })

import { Message } from '@/types/chat'
import { configureMarked } from '@/utils/markdown'
import { extractCodeBlocks } from '@/utils/code'
import { marked } from 'marked'
import { supabase } from '@/utils/supabase'
import { useSearchParams } from 'next/navigation'

// Move localStorage operations to a utility function
const updateChatHistory = (chatId: string) => {
  try {
    if (typeof window === 'undefined') return
    const chatHistory = JSON.parse(localStorage.getItem('chatHistory') || '[]')
    if (!chatHistory.includes(chatId)) {
      localStorage.setItem('chatHistory', JSON.stringify([...chatHistory, chatId]))
    }
  } catch (error) {
    console.warn('Error updating chat history:', error)
  }
}

const ChatPageContent = () => {
  const searchParams = useSearchParams()
  const initialQuestion = searchParams.get('q')
  const chatId = searchParams.get('id')

  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [currentChatId, setCurrentChatId] = useState<string | null>(chatId)
  const [chatTitle, setChatTitle] = useState<string>('')
  const [initialQuestionProcessed, setInitialQuestionProcessed] = useState(false)
  const [streamingMessage, setStreamingMessage] = useState('')
  const [isInitialized, setIsInitialized] = useState(false)
  const [hasCode, setHasCode] = useState(false)
  const [activeTab, setActiveTab] = useState<'code' | 'scene'>('code') // Track active tab
  const [latestCodeBlock, setLatestCodeBlock] = useState<{ code: string, language: string } | null>(null) // Latest code block

  const activeRequestRef = useRef<boolean>(false)

  useEffect(() => {
    if (typeof window !== 'undefined') {
      configureMarked()
    }
  }, [])

  const handleSendMessage = useCallback(async (content: string) => {
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
  }, [currentChatId, messages])

  useEffect(() => {
    const initializeChat = async () => {
      if (!chatId || isInitialized) return
      setIsInitialized(true)

      try {
        const { data: chat } = await supabase
          .from('chats')
          .select('*')
          .eq('id', chatId)
          .single()

        if (chat) {
          setCurrentChatId(chat.id)
          setChatTitle(chat.title)
          updateChatHistory(chat.id)

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

            if (initialQuestion && chatMessages.length === 0 && !initialQuestionProcessed) {
              setInitialQuestionProcessed(true)
              const initialMessage: Message = { role: 'user', content: initialQuestion }
              setMessages([initialMessage])
              handleSendMessage(initialQuestion)
            }
          }
        }
      } catch (error) {
        console.error('Error initializing chat:', error)
      }
    }

    initializeChat()
  }, [chatId, initialQuestion, isInitialized, initialQuestionProcessed, handleSendMessage])

  useEffect(() => {
    if (!chatId) {
      setIsInitialized(false)
      setInitialQuestionProcessed(false)
    }
  }, [chatId])

  useEffect(() => {
    // Extract the latest code block from the assistant's responses
    const lastMessageWithCode = [...messages].reverse().find(message => {
      if (message.role === 'assistant') {
        const blocks = extractCodeBlocks(message.content)
        return blocks.some(block => block.isCode)
      }
      return false
    })

    if (lastMessageWithCode) {
      const blocks = extractCodeBlocks(lastMessageWithCode.content)
      const latestBlock = blocks.find(block => block.isCode)
      if (latestBlock) {
        setLatestCodeBlock({ code: latestBlock.code, language: latestBlock.language })
        setHasCode(true)
      } else {
        setHasCode(false)
      }
    } else {
      setHasCode(false)
      setLatestCodeBlock(null)
    }
  }, [messages])

  const renderMessage = (content: string) => {
    const blocks = extractCodeBlocks(content)

    return blocks.map((block, index) => (
      block.isCode ? (
        <CodeView 
          key={index}
          code={block.code}
          language={block.language}
        />
      ) : (
        <div
          key={index}
          className="prose dark:prose-invert max-w-none"
          dangerouslySetInnerHTML={{ __html: marked(block.content) }}
        />
      )
    ))
  }

  return (
    <div className="min-h-screen bg-slate-900">
      <div className="p-4 w-full">
        <AnimatePresence>
          {chatTitle && (
            <motion.h1 
              initial={{ opacity: 0, y: -20 }}
              animate={{ opacity: 1, y: 0 }}
              className="text-xl font-semibold mb-6 text-white max-w-6xl mx-auto"
            >
              {chatTitle}
            </motion.h1>
          )}
        </AnimatePresence>
        
        <div className={`${hasCode ? 'grid grid-cols-2 gap-4' : 'flex justify-center'} max-w-full`}>
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className={`space-y-4 mb-24 ${hasCode ? 'px-4' : 'w-[60%] px-8 py-6 bg-slate-800/30 rounded-2xl'}`}
          >
            <AnimatePresence>
              {messages.map((message, index) => (
                <motion.div
                  key={index}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -20 }}
                  transition={{ duration: 0.2 }}
                  className={`p-4 rounded-xl max-w-[90%] ${
                    message.role === 'user' 
                      ? 'bg-blue-900/50 text-white ml-auto' 
                      : 'bg-slate-800/50 text-white mr-auto'
                  }`}
                >
                  {message.role === 'assistant' && hasCode ? (
                    <div className="prose dark:prose-invert max-w-none">
                      {extractCodeBlocks(message.content)
                        .filter(block => !block.isCode)
                        .map((block, idx) => (
                          <div
                            key={idx}
                            dangerouslySetInnerHTML={{ __html: marked(block.content) }}
                          />
                        ))}
                    </div>
                  ) : (
                    renderMessage(message.content)
                  )}
                </motion.div>
              ))}
            </AnimatePresence>
            
            <AnimatePresence>
              {streamingMessage && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0 }}
                  className="bg-slate-800/50 p-4 rounded-xl mr-auto max-w-[90%] text-white"
                >
                  {renderMessage(streamingMessage)}
                </motion.div>
              )}
            </AnimatePresence>
            
            <AnimatePresence>
              {isLoading && !streamingMessage && (
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="bg-slate-800/50 p-4 rounded-xl mr-auto max-w-[90%] animate-pulse text-white"
                >
                  Thinking...
                </motion.div>
              )}
            </AnimatePresence>
          </motion.div>

          <AnimatePresence>
            {hasCode && latestCodeBlock && (
              <motion.div
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: 20 }}
                className="h-[calc(100vh-12rem)] bg-slate-800 rounded-xl overflow-hidden sticky top-4"
              >
                <div className="flex border-b border-slate-700">
                  <button
                    onClick={() => setActiveTab('code')}
                    className={`flex-1 p-4 text-center ${activeTab === 'code' ? 'bg-slate-900 text-white' : 'bg-slate-800 text-gray-400'}`}
                  >
                    Code View
                  </button>
                  <button
                    onClick={() => setActiveTab('scene')}
                    className={`flex-1 p-4 text-center ${activeTab === 'scene' ? 'bg-slate-900 text-white' : 'bg-slate-800 text-gray-400'}`}
                  >
                    Scene View
                  </button>
                </div>
                <div className="h-full overflow-auto">
                  {activeTab === 'code' && (
                    <CodeView
                      code={latestCodeBlock.code}
                      language={latestCodeBlock.language}
                    />
                  )}
                  {activeTab === 'scene' && (
                    <SceneView
                      code={latestCodeBlock.code}
                    />
                  )}
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        <motion.form
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          onSubmit={(e) => {
            e.preventDefault()
            handleSendMessage(input)
          }}
          className="fixed bottom-0 left-0 right-0 p-4 bg-slate-800/80 border-t border-slate-700 backdrop-blur-sm"
        >
          <div className="max-w-6xl mx-auto flex gap-2">
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              className="flex-1 p-2 border rounded-xl focus:ring-2 focus:ring-blue-500 outline-none bg-slate-900 text-white border-slate-700"
              placeholder="Type your message..."
              aria-label="Chat message"
            />
            <button
              type="submit"
              className="px-4 py-2 bg-blue-600 text-white rounded-xl hover:bg-blue-700 disabled:opacity-50 disabled:hover:bg-blue-600"
              disabled={isLoading || !input.trim()}
              aria-label="Send message"
            >
              Send
            </button>
          </div>
        </motion.form>
      </div>
    </div>
  )
}

// Wrap the entire page component with dynamic import to disable SSR
const ChatPage = dynamic(() => Promise.resolve(() => (
  <Suspense fallback={
    <div className="min-h-screen bg-slate-900 flex items-center justify-center">
      <div className="text-white">Loading...</div>
    </div>
  }>
    <ChatPageContent />
  </Suspense>
)), { ssr: false })

export default ChatPage
