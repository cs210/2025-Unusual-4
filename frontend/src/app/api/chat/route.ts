import OpenAI from 'openai'
import { NextResponse } from 'next/server'
import { type NextRequest } from 'next/server'

// Define message type
interface ChatMessage {
  role: 'user' | 'assistant' | 'system'
  content: string
}

// Define request body type
interface RequestBody {
  message: string
  messages: ChatMessage[]
}

// Initialize OpenAI client
const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY
})

export async function POST(req: NextRequest) {
  try {
    const { message, messages } = (await req.json()) as RequestBody

    // Create stream
    const stream = await openai.chat.completions.create({
      model: 'gpt-3.5-turbo',
      messages: [
        ...messages.map((msg: ChatMessage) => ({
          role: msg.role,
          content: msg.content
        })),
        { role: 'user', content: message }
      ],
      stream: true
    })

    // Create a new ReadableStream
    const readableStream = new ReadableStream({
      async start(controller) {
        for await (const chunk of stream) {
          const text = chunk.choices[0]?.delta?.content || ''
          controller.enqueue(text)
        }
        controller.close()
      }
    })

    return new Response(readableStream, {
      headers: {
        'Content-Type': 'text/event-stream',
        'Cache-Control': 'no-cache',
        'Connection': 'keep-alive',
      },
    })
  } catch (error) {
    console.error('Error:', error)
    return NextResponse.json(
      { error: 'An error occurred during your request.' },
      { status: 500 }
    )
  }
}