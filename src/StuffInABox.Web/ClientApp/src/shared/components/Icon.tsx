import {
  IconBox, IconHome, IconCar, IconStairs, IconDoor, IconBuildingWarehouse,
  IconTools, IconArchive, IconBooks, IconFridge, IconPlant2, IconBike,
  IconChristmasTree, IconPaint, IconShirt, IconBallBasketball,
} from '@tabler/icons-react'
import type { CSSProperties } from 'react'

// Maps the stored space-icon name strings (e.g. "ti-car") to Tabler SVG
// components. Rendering SVGs avoids depending on the Tabler webfont/CDN at
// runtime, so icons work offline and the CSP can stay tight.
const ICON_MAP: Record<string, typeof IconBox> = {
  'ti-box': IconBox,
  'ti-home': IconHome,
  'ti-car': IconCar,
  'ti-stairs': IconStairs,
  'ti-door': IconDoor,
  'ti-building-warehouse': IconBuildingWarehouse,
  'ti-tools': IconTools,
  'ti-archive': IconArchive,
  'ti-books': IconBooks,
  'ti-fridge': IconFridge,
  'ti-plant-2': IconPlant2,
  'ti-bike': IconBike,
  'ti-christmas-tree': IconChristmasTree,
  'ti-paint': IconPaint,
  'ti-shirt': IconShirt,
  'ti-ball-basketball': IconBallBasketball,
}

interface IconProps {
  name: string
  size?: number
  color?: string
  style?: CSSProperties
}

export function Icon({ name, size = 20, color, style }: IconProps) {
  const Cmp = ICON_MAP[name] ?? IconBox
  return <Cmp size={size} color={color} style={style} />
}
