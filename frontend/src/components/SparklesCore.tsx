"use client"

import type React from "react"
import { useRef, useEffect } from "react"
import { useMousePosition } from "@/lib/hooks/use-mouse-position"

interface SparklesProps {
  background?: string
  minSize?: number
  maxSize?: number
  particleDensity?: number
  className?: string
  particleColor?: string
}

export const SparklesCore: React.FC<SparklesProps> = ({
  background = "transparent",
  minSize = 0.4,
  maxSize = 1,
  particleDensity = 100,
  className = "h-full w-full",
  particleColor = "#FFFFFF",
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const mousePosition = useMousePosition()

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return

    const ctx = canvas.getContext("2d")
    if (!ctx) return

    const dpr = window.devicePixelRatio || 1
    const displayWidth = canvas.clientWidth * dpr
    const displayHeight = canvas.clientHeight * dpr
    canvas.width = displayWidth
    canvas.height = displayHeight

    let animationFrameId: number
    let particles: Particle[] = []

    class Particle {
      x: number
      y: number
      size: number
      speedX: number
      speedY: number

      constructor() {
        this.x = Math.random() * displayWidth
        this.y = Math.random() * displayHeight
        this.size = Math.random() * (maxSize - minSize) + minSize
        this.speedX = Math.random() * 0.5 - 0.25
        this.speedY = Math.random() * 0.5 - 0.25
      }

      update() {
        this.x += this.speedX
        this.y += this.speedY

        if (this.x > displayWidth) this.x = 0
        if (this.x < 0) this.x = displayWidth
        if (this.y > displayHeight) this.y = 0
        if (this.y < 0) this.y = displayHeight

        const dx = mousePosition.x * dpr - this.x
        const dy = mousePosition.y * dpr - this.y
        const distance = Math.sqrt(dx * dx + dy * dy)
        if (distance < 120) {
          const angle = Math.atan2(dy, dx)
          this.x -= Math.cos(angle) * 1
          this.y -= Math.sin(angle) * 1
        }
      }

      draw() {
        if (!ctx) return
        ctx.fillStyle = particleColor
        ctx.beginPath()
        ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2)
        ctx.fill()
      }
    }

    const init = () => {
      particles = []
      for (let i = 0; i < particleDensity; i++) {
        particles.push(new Particle())
      }
    }

    const animate = () => {
      if (!ctx) return
      ctx.clearRect(0, 0, displayWidth, displayHeight)

      particles.forEach((particle) => {
        particle.update()
        particle.draw()
      })

      animationFrameId = requestAnimationFrame(animate)
    }

    init()
    animate()

    return () => {
      cancelAnimationFrame(animationFrameId)
    }
  }, [maxSize, minSize, particleColor, particleDensity, mousePosition.x, mousePosition.y])

  return (
    <canvas
      ref={canvasRef}
      className={className}
      style={{
        background,
      }}
    />
  )
}

