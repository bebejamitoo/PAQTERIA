import { useEffect, useState } from 'react';
import EmptyState from '../components/EmptyState';
import { PageHeader } from './SummaryPage';

const emptyForm = {
    nombre: '',
    email: '',
    telefono: '',
    idRol: '',
    password: '',
};

async function readResponseBody(response) {
    const text = await response.text();
    if (!text.trim()) return null;
    try {
        return JSON.parse(text);
    } catch {
        return { message: text };
    }
}

function UsersPage() {
    const [users, setUsers] = useState([]);
    const [roles, setRoles] = useState([]);
    const [form, setForm] = useState(emptyForm);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [message, setMessage] = useState(null);

    useEffect(() => {
        loadData();
    }, []);

    async function loadData() {
        setIsLoading(true);
        try {
            const [usersResponse, optionsResponse] = await Promise.all([
                fetch('/api/auth/users'),
                fetch('/api/auth/users/options'),
            ]);
            const usersBody = await readResponseBody(usersResponse);
            const optionsBody = await readResponseBody(optionsResponse);
            if (!usersResponse.ok) throw new Error(usersBody?.message ?? `No fue posible consultar los usuarios (${usersResponse.status}).`);
            if (!optionsResponse.ok) throw new Error(optionsBody?.message ?? `No fue posible consultar los roles (${optionsResponse.status}).`);
            setUsers(Array.isArray(usersBody) ? usersBody : []);
            setRoles(optionsBody?.roles ?? []);
        } catch (error) {
            setMessage({ type: 'error', text: error.message });
        } finally {
            setIsLoading(false);
        }
    }

    function handleChange(event) {
        const { name, value } = event.target;
        setForm((current) => ({ ...current, [name]: value }));
    }

    async function handleCreate(event) {
        event.preventDefault();
        if (isSaving) return;

        setIsSaving(true);
        setMessage(null);
        try {
            const response = await fetch('/api/auth/users', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ ...form, idRol: Number(form.idRol) }),
            });
            const body = await readResponseBody(response);
            if (!response.ok) throw new Error(body?.message ?? `No fue posible crear la cuenta (${response.status}).`);
            if (!body) throw new Error('La API no devolvió la cuenta creada.');

            setForm(emptyForm);
            setMessage({ type: 'success', text: `La cuenta de ${body.nombre} se creó correctamente.` });
            await loadData();
        } catch (error) {
            setMessage({ type: 'error', text: error.message });
        } finally {
            setIsSaving(false);
        }
    }

    return <>
        <PageHeader title="Usuarios y roles" subtitle="Crea cuentas y asigna los permisos almacenados en SQL Server." />
        <section className="content-section users-section">
            <form className="user-form" onSubmit={handleCreate}>
                <div className="form-heading"><div><h2>Nueva cuenta</h2><p>Las contraseñas se guardan con hash y nunca se muestran en la lista.</p></div></div>
                <div className="user-form-grid">
                    <label>Nombre completo<input name="nombre" value={form.nombre} onChange={handleChange} required /></label>
                    <label>Correo electrónico<input name="email" type="email" value={form.email} onChange={handleChange} required /></label>
                    <label>Teléfono<input name="telefono" value={form.telefono} onChange={handleChange} /></label>
                    <label>Rol<select name="idRol" value={form.idRol} onChange={handleChange} required><option value="">Selecciona un rol</option>{roles.map((role) => <option key={role.idRol} value={role.idRol}>{role.nombre}</option>)}</select></label>
                    <label>Contraseña inicial<input name="password" type="password" minLength="12" value={form.password} onChange={handleChange} required /><small>Mínimo 12 caracteres.</small></label>
                </div>
                <div className="form-actions"><button className="primary-button" disabled={isSaving} type="submit">{isSaving ? 'Creando...' : 'Crear cuenta'}</button></div>
            </form>
            {message && <div className={`operation-message ${message.type}`} role="status">{message.text}</div>}
            <section className="panel users-table-panel"><div className="table-wrapper"><table><thead><tr><th>Nombre</th><th>Correo</th><th>Teléfono</th><th>Rol</th><th>Registro</th></tr></thead><tbody>{users.map((user) => <tr key={user.idUsuario}><td>{user.nombre}</td><td>{user.email}</td><td>{user.telefono || '—'}</td><td>{user.rol}</td><td>{user.fechaRegistro ? new Date(user.fechaRegistro).toLocaleDateString('es-MX') : '—'}</td></tr>)}</tbody></table></div>{isLoading ? <EmptyState title="Cargando usuarios" description="Consultando cuentas y roles." /> : users.length === 0 && <EmptyState title="Sin usuarios disponibles" description="Las cuentas registradas aparecerán en esta tabla." />}</section>
        </section>
    </>;
}

export default UsersPage;
