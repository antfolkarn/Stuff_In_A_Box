import { Icon } from './Icon'

const ICONS = [
  'ti-box', 'ti-home', 'ti-car', 'ti-stairs', 'ti-door',
  'ti-building-warehouse', 'ti-tools', 'ti-archive', 'ti-books', 'ti-fridge',
  'ti-plant-2', 'ti-bike', 'ti-christmas-tree', 'ti-paint', 'ti-shirt',
  'ti-ball-basketball',
]

interface Props {
  value: string
  onChange: (icon: string) => void
  label?: string
}

export default function SpaceIconPicker({ value, onChange, label }: Props) {
  return (
    <div>
      {label && (
        <div className="field-label" style={{ marginBottom: 8 }}>
          {label}
        </div>
      )}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
        {ICONS.map((icon) => {
          const active = icon === value
          return (
            <button
              key={icon}
              title={icon}
              onClick={() => onChange(icon)}
              style={{
                width: 40,
                height: 40,
                borderRadius: 10,
                border: active
                  ? `2px solid var(--accent)`
                  : '1px solid var(--border)',
                background: active ? 'var(--accent-9)' : 'var(--surface)',
                color: active ? 'var(--accent)' : 'var(--text-2)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: 'pointer',
                transition: 'all 0.12s',
              }}
            >
              <Icon name={icon} size={18} color={active ? 'var(--accent)' : 'var(--text-2)'} />
            </button>
          )
        })}
      </div>
    </div>
  )
}

export { ICONS }
