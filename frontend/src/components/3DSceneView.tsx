"use client"

import type React from "react"
import { useEffect, useRef, useState } from "react"
import * as THREE from "three"
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls"
import { OBJLoader } from "three/examples/jsm/loaders/OBJLoader"
import { FBXLoader } from "three/examples/jsm/loaders/FBXLoader"
import TWEEN from "@tweenjs/tween.js"

interface SceneViewProps {
  code?: string
  file?: File
}

const SceneView: React.FC<SceneViewProps> = ({ code, file }) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const [error, setError] = useState<string | null>(null)

  // Persistent refs for scene, camera, renderer, and controls
  const sceneRef = useRef<THREE.Scene | null>(null)
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null)
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null)
  const controlsRef = useRef<OrbitControls | null>(null)

  useEffect(() => {
    if (!containerRef.current) return

    // Create the scene, camera, renderer, and controls once
    if (!sceneRef.current) {
      sceneRef.current = new THREE.Scene()
      cameraRef.current = new THREE.PerspectiveCamera(
        75,
        containerRef.current.clientWidth / containerRef.current.clientHeight,
        0.1,
        1000,
      )
      rendererRef.current = new THREE.WebGLRenderer({ antialias: true })
      rendererRef.current.setSize(containerRef.current.clientWidth, containerRef.current.clientHeight)

      // Update: Renderer settings for better color accuracy
      rendererRef.current.outputEncoding = THREE.sRGBEncoding
      rendererRef.current.toneMapping = THREE.ACESFilmicToneMapping
      rendererRef.current.toneMappingExposure = 1.2
      rendererRef.current.gammaFactor = 2.2

      containerRef.current.appendChild(rendererRef.current.domElement)

      // Set initial camera position for better 3D viewing
      cameraRef.current.position.set(0, 0, 5)
      cameraRef.current.lookAt(0, 0, 0)

      // Add OrbitControls
      controlsRef.current = new OrbitControls(cameraRef.current, rendererRef.current.domElement)
      controlsRef.current.enableDamping = true
      controlsRef.current.dampingFactor = 0.25
    }

    const scene = sceneRef.current!
    const camera = cameraRef.current!
    const renderer = rendererRef.current!
    const controls = controlsRef.current!

    // Clear existing objects from the scene
    while (scene.children.length > 0) {
      scene.remove(scene.children[0])
    }

    // Update: Enhanced lighting setup
    // Clear existing lights
    scene.children.forEach((child) => {
      if (child instanceof THREE.Light) {
        scene.remove(child)
      }
    })

    // Add a strong directional light
    const directionalLight = new THREE.DirectionalLight(0xffffff, 1)
    directionalLight.position.set(5, 10, 7.5)
    scene.add(directionalLight)

    // Add ambient light
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5)
    scene.add(ambientLight)

    // Add hemisphere light
    const hemisphereLight = new THREE.HemisphereLight(0xffffff, 0x444444, 0.5)
    scene.add(hemisphereLight)

    // Add environment map
    const pmremGenerator = new THREE.PMREMGenerator(renderer)
    pmremGenerator.compileEquirectangularShader()

    new THREE.TextureLoader().load("/path/to/environment_map.jpg", (texture) => {
      const envMap = pmremGenerator.fromEquirectangular(texture).texture
      scene.environment = envMap
      scene.background = envMap

      texture.dispose()
      pmremGenerator.dispose()
    })

    // Helper function to load 3D models
    const loadModel = (url: string, fileType: string) => {
      return new Promise((resolve, reject) => {
        let loader
        if (fileType === "obj") {
          loader = new OBJLoader()
        } else if (fileType === "fbx") {
          loader = new FBXLoader()
        } else {
          reject(new Error("Unsupported file type"))
          return
        }

        loader.load(
          url,
          (object) => {
            resolve(object)
          },
          undefined,
          (error) => {
            reject(error)
          },
        )
      })
    }

    if (file) {
      const fileType = file.name.split(".").pop()?.toLowerCase()
      let loader

      if (fileType === "obj") {
        loader = new OBJLoader()
      } else if (fileType === "fbx") {
        loader = new FBXLoader()
      } else {
        setError("Unsupported file type. Please upload an OBJ or FBX file.")
        return
      }

      const reader = new FileReader()

      reader.onload = (event) => {
        if (event.target?.result) {
          const contents = event.target.result

          let object: THREE.Object3D // Declare object variable

          if (fileType === "obj") {
            object = (loader as OBJLoader).parse(contents as string)
            scene.add(object)
          } else if (fileType === "fbx") {
            object = (loader as FBXLoader).parse(contents as ArrayBuffer, "")
            scene.add(object)
          }

          // Update: Improved material handling for FBX files
          if (fileType === "fbx") {
            scene.traverse((child) => {
              if (child instanceof THREE.Mesh) {
                if (child.material) {
                  // Ensure the material is MeshStandardMaterial
                  if (!(child.material instanceof THREE.MeshStandardMaterial)) {
                    child.material = new THREE.MeshStandardMaterial({
                      color: child.material.color,
                      map: child.material.map,
                    })
                  }

                  // Adjust material properties
                  child.material.side = THREE.DoubleSide
                  child.material.needsUpdate = true
                  child.material.metalness = 0.1
                  child.material.roughness = 0.8

                  // Preserve original colors
                  if (child.material.color) {
                    child.material.color.convertSRGBToLinear()
                  }
                  if (child.material.map) {
                    child.material.map.encoding = THREE.sRGBEncoding
                  }
                }
              }
            })
          }

          // Adjust camera and controls
          const box = new THREE.Box3().setFromObject(scene)
          const center = box.getCenter(new THREE.Vector3())
          const size = box.getSize(new THREE.Vector3())

          const maxDim = Math.max(size.x, size.y, size.z)
          const fov = camera.fov * (Math.PI / 180)
          let cameraZ = Math.abs(maxDim / 2 / Math.tan(fov / 2))
          cameraZ *= 2 // Zoom out more to ensure the entire object is visible

          camera.position.set(center.x, center.y, center.z + cameraZ)
          camera.lookAt(center)
          controls.target.set(center.x, center.y, center.z)
          controls.update()

          // Fit scene to camera
          camera.updateProjectionMatrix()
          controls.saveState()

          // Log camera position and target for debugging
          console.log("Camera position:", camera.position)
          console.log("Controls target:", controls.target)
        }
      }

      reader.onerror = (error) => {
        console.error(`Error reading ${fileType.toUpperCase()} file:`, error)
        setError(`Error reading ${fileType.toUpperCase()} file`)
      }

      if (fileType === "obj") {
        reader.readAsText(file)
      } else if (fileType === "fbx") {
        reader.readAsArrayBuffer(file)
      }
    } else if (code) {
      // Execute the injected code
      try {
        const executeCode = new Function(
          "THREE",
          "scene",
          "camera",
          "renderer",
          "controls",
          "loadModel",
          "TWEEN",
          `
          try {
            ${code}
          } catch (error) {
            console.error("Error executing custom code:", error);
            throw error;
          }
        `,
        )

        executeCode(THREE, scene, camera, renderer, controls, loadModel, TWEEN)
      } catch (error) {
        console.error("Error executing WebGL code:", error)
        setError(error.message)
      }
    }

    // Animation loop
    const animate = () => {
      requestAnimationFrame(animate)
      TWEEN.update()
      controls.update()
      renderer.render(scene, camera)
    }
    animate()

    // Handle window resize
    const handleResize = () => {
      if (containerRef.current) {
        const width = containerRef.current.clientWidth
        const height = containerRef.current.clientHeight
        camera.aspect = width / height
        camera.updateProjectionMatrix()
        renderer.setSize(width, height)
      }
    }

    window.addEventListener("resize", handleResize)

    return () => {
      window.removeEventListener("resize", handleResize)
    }
  }, [code, file])

  if (error) {
    return (
      <div className="text-red-500 p-4">
        <p>Error: {error}</p>
      </div>
    )
  }

  return (
    <div className="relative w-full h-full">
      <div ref={containerRef} className="w-full h-full bg-black rounded-xl" />
    </div>
  )
}

export default SceneView

