import { TestBed } from '@angular/core/testing';
import { AuthService } from '../../../core/services/auth.service';
import { RealtimeBoardService } from './realtime-board.service';

describe('RealtimeBoardService', () => {
    let service: RealtimeBoardService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [RealtimeBoardService, { provide: AuthService, useValue: { getToken: () => 'fake-jwt' } }]
        });
        service = TestBed.inject(RealtimeBoardService);
    });

    // No se prueba el protocolo SignalR en si (biblioteca de terceros, ya probada por
    // Microsoft) sino la capa propia: sin conexion abierta, el servicio debe comportarse
    // de forma segura en vez de lanzar excepciones -- BoardComponent.ngOnDestroy depende
    // de esto cuando el usuario navega antes de que la conexion llegue a establecerse.
    it('connectionId es null antes de conectar', () => {
        expect(service.connectionId).toBeNull();
    });

    it('joinBoard sin conexion abierta no lanza excepcion', async () => {
        await expectAsync(service.joinBoard('proj-1')).toBeResolved();
    });

    it('leaveBoard sin conexion abierta no lanza excepcion', async () => {
        await expectAsync(service.leaveBoard('proj-1')).toBeResolved();
    });

    it('disconnect sin conexion abierta no lanza excepcion', async () => {
        await expectAsync(service.disconnect()).toBeResolved();
    });

    it('connectedUsers$ empieza en una lista vacia', (done) => {
        service.connectedUsers$.subscribe((users) => {
            expect(users).toEqual([]);
            done();
        });
    });

    it('leaveBoard limpia connectedUsers$ aunque no haya conexion abierta', async () => {
        await service.leaveBoard('proj-1');

        service.connectedUsers$.subscribe((users) => expect(users).toEqual([]));
    });
});
